using System.Security.Cryptography.X509Certificates;

namespace OpsIntel.Platform.Abstractions;

/// <summary>
/// Resolves the TLS certificate Kestrel uses for <c>https://localhost:6500</c> (ADR-0003,
/// ADR-0004). Preference order is: enterprise PKI-issued certificate (by thumbprint or
/// subject), falling back to a machine-specific, non-shared-root leaf certificate. No common
/// root CA is ever distributed.
/// </summary>
public interface ICertificateProvider
{
    /// <summary>Resolves the certificate to bind Kestrel's HTTPS endpoint to.</summary>
    Task<X509Certificate2> GetServerCertificateAsync(CancellationToken cancellationToken = default);

    /// <summary>Looks up a certificate by thumbprint in the configured certificate store, or <c>null</c>.</summary>
    Task<X509Certificate2?> FindByThumbprintAsync(string thumbprint, CancellationToken cancellationToken = default);
}
