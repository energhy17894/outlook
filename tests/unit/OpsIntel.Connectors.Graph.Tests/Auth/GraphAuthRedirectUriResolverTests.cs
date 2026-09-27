using OpsIntel.Connectors.Graph.Auth;
using Xunit;

namespace OpsIntel.Connectors.Graph.Tests.Auth;

public sealed class GraphAuthRedirectUriResolverTests
{
    [Fact]
    public void Resolve_NoConfiguredValue_DerivesHttpsUriFromPort()
    {
        var resolved = GraphAuthRedirectUriResolver.Resolve(configuredRedirectUri: null, port: 6500);

        Assert.Equal("https://localhost:6500/auth/callback", resolved);
    }

    [Fact]
    public void Resolve_EmptyConfiguredValue_DerivesFromPort()
    {
        var resolved = GraphAuthRedirectUriResolver.Resolve(configuredRedirectUri: string.Empty, port: 6500);

        Assert.Equal("https://localhost:6500/auth/callback", resolved);
    }

    [Fact]
    public void Resolve_WhitespaceConfiguredValue_DerivesFromPort()
    {
        var resolved = GraphAuthRedirectUriResolver.Resolve(configuredRedirectUri: "   ", port: 6500);

        Assert.Equal("https://localhost:6500/auth/callback", resolved);
    }

    [Fact]
    public void Resolve_UsesTheConfiguredPort_NotTheDefault()
    {
        var resolved = GraphAuthRedirectUriResolver.Resolve(configuredRedirectUri: null, port: 8443);

        Assert.Equal("https://localhost:8443/auth/callback", resolved);
    }

    [Fact]
    public void Resolve_ExplicitlyConfiguredValue_IsReturnedUnchanged_RegardlessOfPort()
    {
        var resolved = GraphAuthRedirectUriResolver.Resolve(
            configuredRedirectUri: "https://contoso.example/custom-callback",
            port: 6500);

        Assert.Equal("https://contoso.example/custom-callback", resolved);
    }

    [Fact]
    public void Resolve_NeverProducesAnHttpUri()
    {
        var resolved = GraphAuthRedirectUriResolver.Resolve(configuredRedirectUri: null, port: 80);

        Assert.StartsWith("https://", resolved);
    }
}
