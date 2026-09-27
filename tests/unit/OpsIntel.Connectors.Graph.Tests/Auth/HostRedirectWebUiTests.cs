using OpsIntel.Connectors.Graph.Auth;
using Xunit;

namespace OpsIntel.Connectors.Graph.Tests.Auth;

/// <summary>
/// Exercises the login hand-off primitive at the heart of the Faz 0 auth spike
/// (<c>GraphLoginSession</c> + <c>HostRedirectWebUi</c>) in isolation from MSAL itself, since
/// MSAL's own builder types can't be faked without a live authority. This proves the
/// synchronization contract: <c>AcquireAuthorizationCodeAsync</c> publishes the authorization
/// URL and then blocks until the callback URL arrives, exactly matching how
/// <c>/auth/login</c> and <c>/auth/callback</c> use it in <c>GraphAuthService</c>.
/// </summary>
public class HostRedirectWebUiTests
{
    [Fact]
    public async Task AcquireAuthorizationCodeAsync_PublishesAuthorizationUri_ThenWaitsForCallback()
    {
        var session = new GraphLoginSession("state-123");
        var webUi = new HostRedirectWebUi(session);

        var authorizationUri = new Uri("https://login.microsoftonline.com/organizations/oauth2/v2.0/authorize?code_challenge=abc");
        var callbackUri = new Uri("http://localhost:6500/auth/callback?code=xyz&state=state-123");

        var acquireTask = webUi.AcquireAuthorizationCodeAsync(authorizationUri, new Uri("http://localhost:6500/auth/callback"), CancellationToken.None);

        // Simulates /auth/login: waits only for the authorization URL, does not block on sign-in completing.
        var published = await session.AuthorizationReady.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(authorizationUri, published);
        Assert.False(acquireTask.IsCompleted, "must still be waiting for the callback");

        // Simulates /auth/callback: hands the browser's redirected-back URL to the waiting call.
        session.CallbackReceived.TrySetResult(callbackUri);

        var result = await acquireTask.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(callbackUri, result);
    }

    [Fact]
    public async Task AcquireAuthorizationCodeAsync_Cancels_WhenTokenCancelled()
    {
        var session = new GraphLoginSession("state-abc");
        var webUi = new HostRedirectWebUi(session);
        using var cts = new CancellationTokenSource();

        var acquireTask = webUi.AcquireAuthorizationCodeAsync(
            new Uri("https://login.microsoftonline.com/organizations/oauth2/v2.0/authorize"),
            new Uri("http://localhost:6500/auth/callback"),
            cts.Token);

        await session.AuthorizationReady.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => acquireTask);
    }

    [Fact]
    public async Task GraphAuthService_CompleteInteractiveLoginAsync_ReportsError_ForUnknownState()
    {
        // No IPublicClientApplication is exercised here on purpose: this asserts the
        // "session not found" branch, which is pure connector-owned coordination logic and
        // never touches MSAL at all — so a null app (which nothing here calls into) is enough.
        var options = Microsoft.Extensions.Options.Options.Create(new GraphAuthOptions { ClientId = "test-client" });
        var service = new GraphAuthService(app: null!, options);

        var outcome = await service.CompleteInteractiveLoginAsync(
            "unknown-state",
            new Uri("http://localhost:6500/auth/callback?code=x&state=unknown-state"));

        Assert.False(outcome.Success);
        Assert.NotNull(outcome.ErrorMessage);
    }
}
