using OpsIntel.Host.Security;
using Xunit;

namespace OpsIntel.UnitTests;

public sealed class OriginCheckMiddlewareTests
{
    private static readonly string[] Allowed = ["localhost", "127.0.0.1", "[::1]"];

    [Theory]
    [InlineData("https://localhost:6500")]
    [InlineData("https://127.0.0.1:6500")]
    public void IsAllowedOrigin_AllowsConfiguredHosts(string origin)
    {
        Assert.True(OriginCheckMiddleware.IsAllowedOrigin(origin, Allowed));
    }

    [Theory]
    [InlineData("https://evil.example.com")]
    [InlineData("not-a-uri")]
    [InlineData("")]
    public void IsAllowedOrigin_RejectsEverythingElse(string origin)
    {
        Assert.False(OriginCheckMiddleware.IsAllowedOrigin(origin, Allowed));
    }
}
