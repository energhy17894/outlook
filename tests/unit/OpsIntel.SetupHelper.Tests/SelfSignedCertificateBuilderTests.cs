using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using OpsIntel.SetupHelper.Cert;
using Xunit;

namespace OpsIntel.SetupHelper.Tests;

/// <summary>
/// Exercises <see cref="SelfSignedCertificateBuilder"/> only — no <see cref="X509Store"/>, no
/// registry, no ACLs — so these assertions hold on every OS/CI runner, not just Windows
/// (ADR-0004's requirements are about the certificate's *content*, which is platform-neutral).
/// </summary>
public sealed class SelfSignedCertificateBuilderTests
{
    private static CertificateRequestParameters DefaultParameters(LeafKeyAlgorithm algorithm = LeafKeyAlgorithm.Rsa2048)
        => new(
            MachineName: "TESTMACHINE01",
            FullyQualifiedDomainName: "testmachine01.corp.example.com",
            KeyAlgorithm: algorithm);

    [Theory]
    [InlineData(LeafKeyAlgorithm.Rsa2048)]
    [InlineData(LeafKeyAlgorithm.EcdsaP256)]
    public void CreateSelfSigned_ProducesCertificateAuthorityFalse(LeafKeyAlgorithm algorithm)
    {
        using var certificate = SelfSignedCertificateBuilder.CreateSelfSigned(DefaultParameters(algorithm), DateTimeOffset.UtcNow);

        Assert.True(SelfSignedCertificateBuilder.IsCertificateAuthorityFalse(certificate));

        var basicConstraints = certificate.Extensions
            .OfType<X509BasicConstraintsExtension>()
            .Single();
        Assert.False(basicConstraints.CertificateAuthority);
        Assert.True(basicConstraints.Critical);
    }

    [Theory]
    [InlineData(LeafKeyAlgorithm.Rsa2048)]
    [InlineData(LeafKeyAlgorithm.EcdsaP256)]
    public void CreateSelfSigned_HasServerAuthenticationEkuOnly(LeafKeyAlgorithm algorithm)
    {
        using var certificate = SelfSignedCertificateBuilder.CreateSelfSigned(DefaultParameters(algorithm), DateTimeOffset.UtcNow);

        Assert.True(SelfSignedCertificateBuilder.HasServerAuthenticationEku(certificate));

        var eku = certificate.Extensions.OfType<X509EnhancedKeyUsageExtension>().Single();
        var oid = Assert.Single(eku.EnhancedKeyUsages.Cast<Oid>());
        Assert.Equal("1.3.6.1.5.5.7.3.1", oid.Value);
    }

    [Fact]
    public void CreateSelfSigned_SubjectAlternativeNames_IncludeLoopbackAndMachineIdentities()
    {
        using var certificate = SelfSignedCertificateBuilder.CreateSelfSigned(DefaultParameters(), DateTimeOffset.UtcNow);

        // Read the typed extension, not X509Extension.Format(): its text is OS- and locale-dependent
        // ("IP Address" vs. "IP Adresi", "::1" vs. "0000:...:0001").
        var san = certificate.Extensions.OfType<X509SubjectAlternativeNameExtension>().Single();
        var dnsNames = san.EnumerateDnsNames().ToList();
        var ipAddresses = san.EnumerateIPAddresses().ToList();

        Assert.Contains("localhost", dnsNames, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("TESTMACHINE01", dnsNames, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("testmachine01.corp.example.com", dnsNames, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(System.Net.IPAddress.Loopback, ipAddresses);
        Assert.Contains(System.Net.IPAddress.IPv6Loopback, ipAddresses);
    }

    [Fact]
    public void CreateSelfSigned_OmitsFqdnSan_WhenFqdnEqualsMachineName()
    {
        var parameters = new CertificateRequestParameters(
            MachineName: "TESTMACHINE01",
            FullyQualifiedDomainName: "TESTMACHINE01");

        using var certificate = SelfSignedCertificateBuilder.CreateSelfSigned(parameters, DateTimeOffset.UtcNow);
        var sanExtension = certificate.Extensions.Single(e => e.Oid?.Value == "2.5.29.17");

        // Exactly one DNS entry for the machine name (no duplicate), plus localhost.
        var occurrences = sanExtension.Format(multiLine: true)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Count(line => line.Contains("TESTMACHINE01", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1, occurrences);
    }

    [Fact]
    public void CreateSelfSigned_DefaultLifetimeIsAboutOneYear()
    {
        var now = DateTimeOffset.UtcNow;
        using var certificate = SelfSignedCertificateBuilder.CreateSelfSigned(DefaultParameters(), now);

        var validity = certificate.NotAfter.ToUniversalTime() - certificate.NotBefore.ToUniversalTime();
        Assert.InRange(validity.TotalDays, 360, 370);
    }

    [Fact]
    public void NeedsRenewal_IsTrue_WithinThirtyDaysOfExpiry()
    {
        var now = DateTimeOffset.UtcNow;
        var parameters = DefaultParameters() with { Lifetime = TimeSpan.FromDays(20) };
        using var certificate = SelfSignedCertificateBuilder.CreateSelfSigned(parameters, now);

        Assert.True(SelfSignedCertificateBuilder.NeedsRenewal(certificate, now, TimeSpan.FromDays(30)));
    }

    [Fact]
    public void NeedsRenewal_IsFalse_ForFreshOneYearCertificate()
    {
        var now = DateTimeOffset.UtcNow;
        using var certificate = SelfSignedCertificateBuilder.CreateSelfSigned(DefaultParameters(), now);

        Assert.False(SelfSignedCertificateBuilder.NeedsRenewal(certificate, now, TimeSpan.FromDays(30)));
    }

    [Fact]
    public void HasServerAuthenticationEku_IsFalse_WhenExtensionListsOtherPurposeOnly()
    {
        var parameters = DefaultParameters();
        var request = SelfSignedCertificateBuilder.CreateRequest(parameters);

        // Replace the serverAuth EKU the builder added with an unrelated one (clientAuth) to
        // simulate a certificate that should fail validation.
        for (var i = request.CertificateExtensions.Count - 1; i >= 0; i--)
        {
            if (request.CertificateExtensions[i] is X509EnhancedKeyUsageExtension)
            {
                request.CertificateExtensions.RemoveAt(i);
            }
        }

        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension([new Oid("1.3.6.1.5.5.7.3.2", "Client Authentication")], critical: false));

        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddDays(365));

        Assert.False(SelfSignedCertificateBuilder.HasServerAuthenticationEku(certificate));
    }
}
