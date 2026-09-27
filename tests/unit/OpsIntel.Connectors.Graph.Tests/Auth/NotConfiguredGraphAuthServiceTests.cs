using OpsIntel.Connectors.Graph.Auth;
using Xunit;

namespace OpsIntel.Connectors.Graph.Tests.Auth;

public sealed class NotConfiguredGraphAuthServiceTests
{
    // Internal type: constructible here via this assembly's InternalsVisibleTo grant on
    // OpsIntel.Connectors.Graph, mirroring how DI resolves IGraphAuthService when ClientId is
    // empty (GraphAuthServiceCollectionExtensions.AddOpsIntelGraphAuth).
    private static IGraphAuthService CreateSut() => new NotConfiguredGraphAuthService();

    [Fact]
    public async Task BeginInteractiveLoginAsync_Throws_NotConfiguredException()
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<GraphAuthNotConfiguredException>(
            () => sut.BeginInteractiveLoginAsync("state"));
    }

    [Fact]
    public async Task GetAccessTokenAsync_Throws_NotConfiguredException()
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<GraphAuthNotConfiguredException>(
            () => sut.GetAccessTokenAsync());
    }

    [Fact]
    public async Task CompleteInteractiveLoginAsync_Throws_NotConfiguredException()
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<GraphAuthNotConfiguredException>(
            () => sut.CompleteInteractiveLoginAsync("state", new Uri("https://localhost:6500/auth/callback?code=x&state=state")));
    }

    [Fact]
    public async Task GetCurrentAccountAsync_ReturnsNull_RatherThanThrowing()
    {
        var sut = CreateSut();

        var account = await sut.GetCurrentAccountAsync();

        Assert.Null(account);
    }

    [Fact]
    public async Task SignOutAsync_CompletesSuccessfully_RatherThanThrowing()
    {
        var sut = CreateSut();

        await sut.SignOutAsync();
    }
}
