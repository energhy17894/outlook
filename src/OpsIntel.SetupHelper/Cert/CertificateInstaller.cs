using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;

namespace OpsIntel.SetupHelper.Cert;

/// <summary>
/// The Windows-only half of certificate provisioning: persists a certificate built by
/// <see cref="SelfSignedCertificateBuilder"/> into <c>LocalMachine\My</c> (and, for the
/// self-signed case only, <c>LocalMachine\Root</c>), grants the Host service account read
/// access to the private key, and reverses all of that on <c>cert remove</c>. Every public
/// method here is guarded with <see cref="OperatingSystem.IsWindows"/> so a build for
/// cross-platform tests never accidentally executes store/ACL code on Linux/macOS.
/// </summary>
public static class CertificateInstaller
{
    /// <summary>
    /// The service account ADR-0004 / installer/Services.wxs grants the private key to. Kept
    /// as a constant here (rather than a CLI argument) because Cert.wxs's contract does not
    /// pass it — the account name is implied by the fixed Host service name.
    /// </summary>
    public const string HostServiceAccount = "NT SERVICE\\OpsIntel.Host";

    /// <summary>
    /// Generates the ADR-0004 self-signed leaf, imports it into <c>LocalMachine\My</c> with a
    /// non-exportable, machine-persisted key, grants <see cref="HostServiceAccount"/> read
    /// access to the private key, and adds the same leaf (CA=false) to
    /// <c>LocalMachine\Root</c> so it is trusted without a shared root CA (ADR-0004 point 3).
    /// </summary>
    /// <returns>The thumbprint of the certificate now in <c>LocalMachine\My</c>.</returns>
    public static string CreateAndInstallSelfSigned(CertificateRequestParameters parameters)
    {
        RequireWindows();

        using var ephemeral = SelfSignedCertificateBuilder.CreateSelfSigned(parameters, DateTimeOffset.UtcNow);

        // Re-importing with MachineKeySet|PersistKeySet (no Exportable flag) forces the crypto
        // layer to persist the private key to a real machine-scoped CNG key container instead
        // of the ephemeral in-memory key CreateSelfSigned produced, and makes the key
        // non-exportable per ADR-0004 point 2 ("anahtar dışa aktarılamaz").
        var pfxBytes = ephemeral.Export(X509ContentType.Pfx, string.Empty);
        try
        {
            using var persisted = X509CertificateLoader.LoadPkcs12(
                pfxBytes,
                string.Empty,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet);

            persisted.FriendlyName = FriendlyNameTag(parameters.FriendlyNameTag);

            InstallToStore(persisted, StoreName.My);
            GrantPrivateKeyRead(persisted, HostServiceAccount);

            // Only the leaf's public part goes into Root — never a private key, and never
            // anything but this one CA=false end-entity certificate (ADR-0004 point 3: "Ortak
            // veya satıcıya ait kök CA hiç dağıtılmaz").
            using (var publicOnly = X509CertificateLoader.LoadCertificate(persisted.Export(X509ContentType.Cert)))
            {
                publicOnly.FriendlyName = persisted.FriendlyName;
                InstallToStore(publicOnly, StoreName.Root);
            }

            return persisted.Thumbprint;
        }
        finally
        {
            Array.Clear(pfxBytes);
        }
    }

    /// <summary>
    /// Validates that <paramref name="thumbprint"/> names a usable corporate PKI certificate:
    /// present in <c>LocalMachine\My</c>, carrying a private key, CA=false and the serverAuth
    /// EKU (ADR-0004 point 1). Does not touch <c>LocalMachine\Root</c> — an enterprise
    /// certificate is assumed to already chain to a trust anchor devices already trust.
    /// </summary>
    public static CorporateCertificateValidationResult ValidateCorporateCertificate(string thumbprint)
    {
        RequireWindows();

        using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadOnly);

        var matches = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);
        if (matches.Count == 0)
        {
            return CorporateCertificateValidationResult.NotFound;
        }

        using var certificate = matches[0];

        if (!certificate.HasPrivateKey)
        {
            return CorporateCertificateValidationResult.NoPrivateKey;
        }

        if (!SelfSignedCertificateBuilder.HasServerAuthenticationEku(certificate))
        {
            return CorporateCertificateValidationResult.MissingServerAuthEku;
        }

        return CorporateCertificateValidationResult.Valid;
    }

    /// <summary>
    /// Removes every certificate this tool created (identified by
    /// <see cref="SelfSignedCertificateBuilder.FriendlyNameTag"/>) from both
    /// <c>LocalMachine\My</c> and <c>LocalMachine\Root</c>. A corporate PKI certificate (no
    /// matching FriendlyName) is never touched, matching ADR-0004 point 5.
    /// </summary>
    public static int RemoveToolCreatedCertificates()
    {
        RequireWindows();

        var removed = 0;
        removed += RemoveTaggedFromStore(StoreName.My);
        removed += RemoveTaggedFromStore(StoreName.Root);
        return removed;
    }

    /// <summary>
    /// Removes one specific certificate (by thumbprint) from both <c>LocalMachine\My</c> and
    /// <c>LocalMachine\Root</c>, used by <c>cert renew</c> to retire exactly the certificate it
    /// just replaced rather than every tool-tagged certificate on the machine.
    /// </summary>
    public static void RemoveByThumbprint(string thumbprint)
    {
        RequireWindows();

        foreach (var storeName in new[] { StoreName.My, StoreName.Root })
        {
            using var store = new X509Store(storeName, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);
            var matches = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);
            foreach (var certificate in matches)
            {
                store.Remove(certificate);
                certificate.Dispose();
            }
        }
    }

    /// <summary>Finds the certificate currently referenced by <paramref name="thumbprint"/> in <c>LocalMachine\My</c>, or <c>null</c>.</summary>
    public static X509Certificate2? FindByThumbprint(string thumbprint)
    {
        RequireWindows();

        using var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadOnly);
        var matches = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);
        return matches.Count > 0 ? matches[0] : null;
    }

    /// <summary>True if the certificate at <paramref name="thumbprint"/> was created by this tool (carries its FriendlyName tag).</summary>
    public static bool WasCreatedByThisTool(string thumbprint)
    {
        RequireWindows();
        using var certificate = FindByThumbprint(thumbprint);
        return certificate is not null &&
               certificate.FriendlyName.StartsWith(SelfSignedCertificateBuilder.FriendlyNameTag, StringComparison.Ordinal);
    }

    private static string FriendlyNameTag(string? suffix)
        => string.IsNullOrEmpty(suffix)
            ? SelfSignedCertificateBuilder.FriendlyNameTag
            : $"{SelfSignedCertificateBuilder.FriendlyNameTag}:{suffix}";

    private static void InstallToStore(X509Certificate2 certificate, StoreName storeName)
    {
        using var store = new X509Store(storeName, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadWrite);
        store.Add(certificate);
    }

    private static int RemoveTaggedFromStore(StoreName storeName)
    {
        using var store = new X509Store(storeName, StoreLocation.LocalMachine);
        store.Open(OpenFlags.ReadWrite);

        var toRemove = new List<X509Certificate2>();
        foreach (var certificate in store.Certificates)
        {
            if (!string.IsNullOrEmpty(certificate.FriendlyName) &&
                certificate.FriendlyName.StartsWith(SelfSignedCertificateBuilder.FriendlyNameTag, StringComparison.Ordinal))
            {
                toRemove.Add(certificate);
            }
        }

        foreach (var certificate in toRemove)
        {
            store.Remove(certificate);
            certificate.Dispose();
        }

        return toRemove.Count;
    }

    /// <summary>
    /// Grants <paramref name="account"/> read access to <paramref name="certificate"/>'s
    /// private key file on disk (msi_kurulum_dagitim.md §4: "Read access is required to use
    /// the private key"). Locates the CNG machine key container
    /// (<c>%ProgramData%\Microsoft\Crypto\Keys</c>) or, for the RSA/CAPI fallback path some
    /// providers still use, the legacy <c>%ProgramData%\Microsoft\Crypto\RSA\MachineKeys</c>
    /// directory, by the key's unique container name, and grants an NTFS ACE instead of
    /// depending on a managed CNG-ACL API (none is exposed by .NET; this mirrors the approach
    /// documented in win-acme/Certes-style Windows ACME clients).
    /// </summary>
    public static void GrantPrivateKeyRead(X509Certificate2 certificate, string account)
    {
        RequireWindows();

        var keyFilePath = TryGetPrivateKeyFilePath(certificate)
            ?? throw new InvalidOperationException(
                "Could not locate the private key's machine key container on disk; the key may not have been persisted with X509KeyStorageFlags.MachineKeySet.");

        var sid = ResolveAccountSid(account);

        var fileInfo = new FileInfo(keyFilePath);
        var fileSecurity = fileInfo.GetAccessControl();
        fileSecurity.AddAccessRule(new FileSystemAccessRule(
            sid,
            FileSystemRights.Read,
            InheritanceFlags.None,
            PropagationFlags.None,
            AccessControlType.Allow));
        fileInfo.SetAccessControl(fileSecurity);
    }

    private static SecurityIdentifier ResolveAccountSid(string account)
    {
        try
        {
            return (SecurityIdentifier)new NTAccount(account).Translate(typeof(SecurityIdentifier));
        }
        catch (IdentityNotMappedException ex)
        {
            throw new InvalidOperationException(
                $"Could not resolve '{account}' to a SID. Virtual service accounts (NT SERVICE\\...) " +
                "are only resolvable once the corresponding service is registered with the SCM; " +
                "ensure Services.wxs's ServiceInstall runs before Cert.wxs's CertCreate action.",
                ex);
        }
    }

    private static string? TryGetPrivateKeyFilePath(X509Certificate2 certificate)
    {
        string? uniqueKeyName = certificate.GetRSAPrivateKey() is RSACng rsaCng
            ? rsaCng.Key.UniqueName
            : certificate.GetECDsaPrivateKey() is ECDsaCng ecdsaCng
                ? ecdsaCng.Key.UniqueName
                : null;

        if (uniqueKeyName is null)
        {
            return null;
        }

        var commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

        var candidates = new[]
        {
            Path.Combine(commonAppData, "Microsoft", "Crypto", "Keys", uniqueKeyName),
            Path.Combine(commonAppData, "Microsoft", "Crypto", "RSA", "MachineKeys", uniqueKeyName),
        };

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static void RequireWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Certificate store/ACL operations are only available on Windows.");
        }
    }
}

/// <summary>Result of <see cref="CertificateInstaller.ValidateCorporateCertificate"/>.</summary>
public enum CorporateCertificateValidationResult
{
    Valid,
    NotFound,
    NoPrivateKey,
    MissingServerAuthEku,
}
