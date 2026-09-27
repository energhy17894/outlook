using OpsIntel.SetupHelper.Cert;
using OpsIntel.SetupHelper.Cli;
using OpsIntel.SetupHelper.Config;

namespace OpsIntel.SetupHelper.Commands;

/// <summary>
/// Implements the three <c>cert</c> sub-commands installer/Cert.wxs invokes as deferred,
/// <c>Impersonate="no"</c> custom actions (ADR-0004). Every method returns an
/// <see cref="ExitCodes"/> value and never throws to its caller — <see cref="Program"/> is the
/// only place stdout/exit code actually reach WiX/msiexec.
/// </summary>
public static class CertCommands
{
    /// <summary>ADR-0004 point 4: renew once fewer than this many days remain.</summary>
    public static readonly TimeSpan RenewalThreshold = TimeSpan.FromDays(30);

    /// <summary>
    /// <c>cert create --port=6500 [--thumbprint=&lt;corp cert&gt;]</c>. If a thumbprint is
    /// given, it must already exist in <c>LocalMachine\My</c> with a private key and the
    /// serverAuth EKU (ADR-0004 point 1); otherwise a machine-specific self-signed leaf is
    /// generated, installed, ACL'd and trusted (ADR-0004 point 2-3). Either way the resulting
    /// thumbprint is written to <c>HKLM\SOFTWARE\OpsIntel\CertThumbprint</c>.
    /// Idempotent: re-running with the same inputs when a matching certificate is already
    /// provisioned is a no-op success.
    /// </summary>
    public static int Create(string? thumbprintArg, int port, TextWriter output)
    {
        if (!OperatingSystem.IsWindows())
        {
            output.WriteLine("cert create is only supported on Windows.");
            return ExitCodes.UnsupportedPlatform;
        }

        if (!string.IsNullOrWhiteSpace(thumbprintArg))
        {
            var validation = CertificateInstaller.ValidateCorporateCertificate(thumbprintArg);
            switch (validation)
            {
                case CorporateCertificateValidationResult.NotFound:
                    output.WriteLine($"No certificate with the given thumbprint was found in LocalMachine\\My (port {port}).");
                    return ExitCodes.CertificateNotFound;
                case CorporateCertificateValidationResult.NoPrivateKey:
                    output.WriteLine("The given thumbprint has no usable private key.");
                    return ExitCodes.CertificateInvalid;
                case CorporateCertificateValidationResult.MissingServerAuthEku:
                    output.WriteLine("The given thumbprint does not carry the Server Authentication EKU.");
                    return ExitCodes.CertificateInvalid;
            }

            OpsIntelRegistryConfig.SetCertThumbprint(thumbprintArg);
            output.WriteLine("Corporate PKI certificate registered.");
            return ExitCodes.Success;
        }

        // Idempotency: if a self-signed cert we created is already registered and still valid
        // for this machine, re-running `cert create` (e.g. a repair/reinstall) should not mint
        // a second one.
        var existingThumbprint = OpsIntelRegistryConfig.GetCertThumbprint();
        if (!string.IsNullOrWhiteSpace(existingThumbprint) && CertificateInstaller.WasCreatedByThisTool(existingThumbprint))
        {
            using var existing = CertificateInstaller.FindByThumbprint(existingThumbprint);
            if (existing is not null && !SelfSignedCertificateBuilder.NeedsRenewal(existing, DateTimeOffset.UtcNow, RenewalThreshold))
            {
                output.WriteLine("A valid self-signed certificate is already provisioned; nothing to do.");
                return ExitCodes.Success;
            }
        }

        var parameters = new CertificateRequestParameters(
            MachineName: Environment.MachineName,
            FullyQualifiedDomainName: TryGetFqdn());

        var thumbprint = CertificateInstaller.CreateAndInstallSelfSigned(parameters);
        OpsIntelRegistryConfig.SetCertThumbprint(thumbprint);

        output.WriteLine("Self-signed certificate provisioned and trusted for this machine.");
        return ExitCodes.Success;
    }

    /// <summary>
    /// <c>cert remove --port=6500</c>. Removes only certificates this tool created (tagged via
    /// FriendlyName), from both <c>LocalMachine\My</c> and <c>LocalMachine\Root</c>, and clears
    /// the registry thumbprint. A corporate PKI certificate is left untouched (ADR-0004 point
    /// 5) — it was never ours to remove. Idempotent: safe to call when nothing is provisioned.
    /// </summary>
    public static int Remove(TextWriter output)
    {
        if (!OperatingSystem.IsWindows())
        {
            output.WriteLine("cert remove is only supported on Windows.");
            return ExitCodes.UnsupportedPlatform;
        }

        var removed = CertificateInstaller.RemoveToolCreatedCertificates();
        OpsIntelRegistryConfig.ClearCertThumbprint();

        output.WriteLine($"Removed {removed} tool-created certificate(s).");
        return ExitCodes.Success;
    }

    /// <summary>
    /// <c>cert renew</c>: the SYSTEM scheduled task's monthly entry point (ADR-0004 point 4).
    /// Renews only a self-signed certificate this tool created, and only once fewer than
    /// <see cref="RenewalThreshold"/> remain before expiry; a corporate PKI certificate is left
    /// for the enterprise's own renewal process. Trusts the new leaf, then removes the old
    /// self-signed leaf from Root/My so a stale trust anchor doesn't accumulate.
    /// </summary>
    public static int Renew(TextWriter output)
    {
        if (!OperatingSystem.IsWindows())
        {
            output.WriteLine("cert renew is only supported on Windows.");
            return ExitCodes.UnsupportedPlatform;
        }

        var thumbprint = OpsIntelRegistryConfig.GetCertThumbprint();
        if (string.IsNullOrWhiteSpace(thumbprint))
        {
            output.WriteLine("No certificate is currently provisioned; nothing to renew.");
            return ExitCodes.CertificateNotFound;
        }

        if (!CertificateInstaller.WasCreatedByThisTool(thumbprint))
        {
            output.WriteLine("The provisioned certificate is a corporate PKI certificate; renewal is the enterprise's responsibility.");
            return ExitCodes.Success;
        }

        using var current = CertificateInstaller.FindByThumbprint(thumbprint);
        if (current is null)
        {
            output.WriteLine("The registered thumbprint no longer resolves to a certificate; re-run 'cert create'.");
            return ExitCodes.CertificateNotFound;
        }

        if (!SelfSignedCertificateBuilder.NeedsRenewal(current, DateTimeOffset.UtcNow, RenewalThreshold))
        {
            output.WriteLine("Certificate is not yet within the renewal window; nothing to do.");
            return ExitCodes.Success;
        }

        var parameters = new CertificateRequestParameters(
            MachineName: Environment.MachineName,
            FullyQualifiedDomainName: TryGetFqdn());

        var newThumbprint = CertificateInstaller.CreateAndInstallSelfSigned(parameters);
        OpsIntelRegistryConfig.SetCertThumbprint(newThumbprint);

        // Remove only the specific old certificate (by thumbprint), not every tool-tagged
        // certificate, so a renewal never clobbers a second machine's cert in a shared store
        // scenario or races a concurrent create.
        CertificateInstaller.RemoveByThumbprint(thumbprint);

        output.WriteLine($"Renewed certificate; new thumbprint registered (old {thumbprint} removed).");
        return ExitCodes.Success;
    }

    private static string? TryGetFqdn()
    {
        try
        {
            var hostEntry = System.Net.Dns.GetHostEntry(Environment.MachineName);
            return string.IsNullOrWhiteSpace(hostEntry.HostName) ? null : hostEntry.HostName;
        }
        catch (System.Net.Sockets.SocketException)
        {
            return null;
        }
    }
}
