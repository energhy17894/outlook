using OpsIntel.Host.Security;
using Xunit;

namespace OpsIntel.UnitTests;

public sealed class HostAllowlistMiddlewareTests
{
    private static readonly string[] Allowed = ["localhost", "127.0.0.1", "[::1]"];

    [Theory]
    [InlineData("localhost")]
    [InlineData("LOCALHOST")]
    [InlineData("127.0.0.1")]
    [InlineData("[::1]")]
    public void IsAllowedHost_AllowsConfiguredNames(string host)
    {
        Assert.True(HostAllowlistMiddleware.IsAllowedHost(host, Allowed));
    }

    [Theory]
    [InlineData("evil.example.com")]
    [InlineData("127.0.0.1.evil.example.com")]
    [InlineData("")]
    public void IsAllowedHost_RejectsEverythingElse(string host)
    {
        Assert.False(HostAllowlistMiddleware.IsAllowedHost(host, Allowed));
    }
}
