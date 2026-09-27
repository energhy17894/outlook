using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Options;
using OpsIntel.Platform.Abstractions;

namespace OpsIntel.Platform.Windows;

/// <summary>Configures <see cref="WindowsCertificateProvider"/>.</summary>
public sealed class WindowsCertificateProviderOptions
{
    /// <summary>Certificate store location to search. Defaults to <c>LocalMachine\My</c> (ADR-0004).</summary>
    public StoreLocation StoreLocation { get; set; } = StoreLocation.LocalMachine;

    /// <summary>Store name to search. Defaults to <see cref="StoreName.My"/>.</summary>
    public StoreName StoreName { get; set; } = StoreName.My;

    /// <summary>Preferred lookup: an exact thumbprint. Takes precedence over <see cref="Subject"/>.</summary>
    public string? Thumbprint { get; set; }

    /// <summary>Fallback lookup: a certificate subject name (e.g. the machine's local hostname).</summary>
    public string? Subject { get; set; }
}

/// <summary>
/// <see cref="ICertificateProvider"/> that loads the Kestrel HTTPS certificate from
/// <c>LocalMachine\My</c> (or a configured store) by thumbprint or subject. No common root CA
/// is bundled: this only ever finds a certificate that was separately provisioned (enterprise
/// PKI or the setup-time machine-specific leaf certificate) — see ADR-0004.
/// </summary>
public sealed class WindowsCertificateProvider : ICertificateProvider
{
    private readonly WindowsCertificateProviderOptions _options;

    public WindowsCertificateProvider(IOptions<WindowsCertificateProviderOptions> options)
    {
        _options = options.Value;
    }

    public Task<X509Certificate2> GetServerCertificateAsync(CancellationToken cancellationToken = default)
    {
        using var store = new X509Store(_options.StoreName, _options.StoreLocation);
        store.Open(OpenFlags.ReadOnly);

        var certificate =
            FindByThumbprint(store, _options.Thumbprint) ??
            FindBySubject(store, _options.Subject) ??
            throw new InvalidOperationException(
                $"No server certificate found in {_options.StoreLocation}\\{_options.StoreName} " +
                $"matching thumbprint '{_options.Thumbprint}' or subject '{_options.Subject}'.");

        return Task.FromResult(certificate);
    }

    public Task<X509Certificate2?> FindByThumbprintAsync(string thumbprint, CancellationToken cancellationToken = default)
    {
        using var store = new X509Store(_options.StoreName, _options.StoreLocation);
        store.Open(OpenFlags.ReadOnly);
        return Task.FromResult(FindByThumbprint(store, thumbprint));
    }

    private static X509Certificate2? FindByThumbprint(X509Store store, string? thumbprint)
    {
        if (string.IsNullOrWhiteSpace(thumbprint))
        {
            return null;
        }

        var matches = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false);
        return matches.Count > 0 ? matches[0] : null;
    }

    private static X509Certificate2? FindBySubject(X509Store store, string? subject)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            return null;
        }

        var matches = store.Certificates.Find(X509FindType.FindBySubjectName, subject, validOnly: false);
        return matches.Count > 0 ? matches[0] : null;
    }
}
