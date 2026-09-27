using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace OpsIntel.SetupHelper.Cert;

/// <summary>
/// Signing key algorithm for a generated leaf certificate.
/// </summary>
public enum LeafKeyAlgorithm
{
    Rsa2048,
    EcdsaP256,
}

/// <summary>
/// Parameters for <see cref="SelfSignedCertificateBuilder.Build"/>.
/// </summary>
/// <param name="MachineName">
/// The local, non-FQDN machine name (<c>Environment.MachineName</c>). Included as a DNS SAN
/// entry alongside <c>localhost</c> per ADR-0004.
/// </param>
/// <param name="FullyQualifiedDomainName">
/// The machine's FQDN, if resolvable and different from <paramref name="MachineName"/>. May be
/// <c>null</c> on a workgroup machine with no usable DNS suffix.
/// </param>
public sealed record CertificateRequestParameters(
    string MachineName,
    string? FullyQualifiedDomainName,
    LeafKeyAlgorithm KeyAlgorithm = LeafKeyAlgorithm.Rsa2048,
    TimeSpan? Lifetime = null,
    string? FriendlyNameTag = null);

/// <summary>
/// Builds the machine-specific, self-signed HTTPS leaf certificate ADR-0004 calls for when no
/// corporate PKI thumbprint is configured. This class is deliberately free of any
/// Windows-only API (no <see cref="X509Store"/>, no registry, no ACL calls) so it can be
/// exercised by unit tests on any OS; <see cref="CertificateInstaller"/> is the Windows-only
/// half that persists the result into <c>LocalMachine\My</c>/<c>LocalMachine\Root</c>.
/// </summary>
public static class SelfSignedCertificateBuilder
{
    /// <summary>Default validity: "~1 year", matching <c>New-SelfSignedCertificate</c>'s default (ADR-0004).</summary>
    public static readonly TimeSpan DefaultLifetime = TimeSpan.FromDays(365);

    /// <summary>
    /// The tag written to <see cref="X509Certificate2.FriendlyName"/> so a later <c>cert
    /// remove</c>/<c>cert renew</c> can identify certificates this tool created, as opposed to
    /// an enterprise PKI certificate or anything else that happens to sit in the same store
    /// (ADR-0004 point 5: "Kaldırmada sertifika silinir" only for what was provisioned here).
    /// </summary>
    public const string FriendlyNameTag = "OpsIntel.SetupHelper";

    /// <summary>OID for the "TLS Web Server Authentication" extended key usage.</summary>
    private const string ServerAuthenticationOid = "1.3.6.1.5.5.7.3.1";

    /// <summary>
    /// Builds a CA=false leaf certificate for <c>https://localhost:6500</c>-style loopback
    /// (and, when <c>ALLOW_LAN=1</c>, LAN) access:
    /// <list type="bullet">
    /// <item>Subject/SAN: <c>localhost</c>, <c>127.0.0.1</c>, <c>::1</c>, the machine name, and
    /// the FQDN when known.</item>
    /// <item>EKU: serverAuth only.</item>
    /// <item>Basic Constraints: <c>CA=false</c>, no path length constraint — this certificate
    /// can never be used to sign another certificate, even though it will be trusted as a root
    /// anchor by <see cref="CertificateInstaller"/> (ADR-0004's documented trade-off).</item>
    /// <item>Key usage: digital signature + key encipherment (RSA) / digital signature (ECDSA).</item>
    /// </list>
    /// </summary>
    public static CertificateRequest CreateRequest(CertificateRequestParameters parameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parameters.MachineName);

        var subjectName = new X500DistinguishedName($"CN={parameters.MachineName}");

        CertificateRequest request = parameters.KeyAlgorithm switch
        {
            LeafKeyAlgorithm.EcdsaP256 => new CertificateRequest(
                subjectName,
                ECDsa.Create(ECCurve.NamedCurves.nistP256),
                HashAlgorithmName.SHA256),
            _ => new CertificateRequest(
                subjectName,
                RSA.Create(2048),
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1),
        };

        // CA=false, critical: this leaf can never sign another certificate (ADR-0004).
        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(certificateAuthority: false, hasPathLengthConstraint: false, pathLengthConstraint: 0, critical: true));

        var keyUsage = parameters.KeyAlgorithm == LeafKeyAlgorithm.EcdsaP256
            ? X509KeyUsageFlags.DigitalSignature
            : X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment;
        request.CertificateExtensions.Add(new X509KeyUsageExtension(keyUsage, critical: true));

        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                [new Oid(ServerAuthenticationOid, "Server Authentication")], critical: false));

        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName("localhost");
        sanBuilder.AddIpAddress(System.Net.IPAddress.Loopback);
        sanBuilder.AddIpAddress(System.Net.IPAddress.IPv6Loopback);
        sanBuilder.AddDnsName(parameters.MachineName);

        if (!string.IsNullOrWhiteSpace(parameters.FullyQualifiedDomainName) &&
            !string.Equals(parameters.FullyQualifiedDomainName, parameters.MachineName, StringComparison.OrdinalIgnoreCase))
        {
            sanBuilder.AddDnsName(parameters.FullyQualifiedDomainName);
        }

        request.CertificateExtensions.Add(sanBuilder.Build(critical: false));

        // Subject Key Identifier lets renewal/trust tooling reason about "is this the same
        // logical key" without relying on the (rotating) thumbprint alone.
        request.CertificateExtensions.Add(
            new X509SubjectKeyIdentifierExtension(request.PublicKey, critical: false));

        return request;
    }

    /// <summary>
    /// Creates the self-signed certificate itself (still entirely in memory — not persisted to
    /// any store). <see cref="X509Certificate2.FriendlyName"/> is left unset here because that
    /// property is a Windows-only certificate-store attribute that throws
    /// <see cref="PlatformNotSupportedException"/> on non-Windows platforms; callers on Windows
    /// set it (to <see cref="FriendlyNameTag"/>, optionally suffixed) after re-importing the
    /// certificate into a real store, which is the only place the attribute is meaningful.
    /// </summary>
    public static X509Certificate2 CreateSelfSigned(CertificateRequestParameters parameters, DateTimeOffset notBeforeUtc)
    {
        var request = CreateRequest(parameters);
        var lifetime = parameters.Lifetime ?? DefaultLifetime;

        // Back-date slightly to tolerate modest clock skew between the machine that generated
        // the certificate and whichever browser/service first validates it.
        var notBefore = notBeforeUtc.AddMinutes(-5);
        var notAfter = notBeforeUtc + lifetime;

        return request.CreateSelfSigned(notBefore, notAfter);
    }

    /// <summary>True when a certificate is within <paramref name="threshold"/> of expiry (ADR-0004: renew inside 30 days).</summary>
    public static bool NeedsRenewal(X509Certificate2 certificate, DateTimeOffset nowUtc, TimeSpan threshold)
        => certificate.NotAfter.ToUniversalTime() - nowUtc.UtcDateTime < threshold;

    /// <summary>True if the certificate carries the serverAuth EKU (or no EKU restriction at all).</summary>
    public static bool HasServerAuthenticationEku(X509Certificate2 certificate)
    {
        foreach (var extension in certificate.Extensions)
        {
            if (extension is X509EnhancedKeyUsageExtension eku)
            {
                if (eku.EnhancedKeyUsages.Count == 0)
                {
                    // An empty EKU extension technically means "any purpose"; be conservative
                    // and still require serverAuth to be explicit for a certificate we validate.
                    return false;
                }

                foreach (var usage in eku.EnhancedKeyUsages)
                {
                    if (string.Equals(usage.Value, ServerAuthenticationOid, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        // No EKU extension at all: X.509 treats this as "valid for any purpose".
        return true;
    }

    /// <summary>True if the certificate's Basic Constraints extension marks it <c>CA=false</c> (or omits the extension, which defaults to an end-entity certificate).</summary>
    public static bool IsCertificateAuthorityFalse(X509Certificate2 certificate)
    {
        foreach (var extension in certificate.Extensions)
        {
            if (extension is X509BasicConstraintsExtension basicConstraints)
            {
                return !basicConstraints.CertificateAuthority;
            }
        }

        return true;
    }
}
