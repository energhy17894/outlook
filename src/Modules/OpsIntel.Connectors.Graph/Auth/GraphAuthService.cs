using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;

namespace OpsIntel.Connectors.Graph.Auth;

/// <summary>
/// Drives the Entra public-client + PKCE sign-in through MSAL.NET's
/// <see cref="ICustomWebUi"/> extensibility point, so a background Windows service (where WAM
/// does not work, per ADR-0007) can still get delegated tokens through a browser-based BFF flow
/// hosted in the same process. See <c>SPIKE-AUTH.md</c> for the full write-up.
/// </summary>
public sealed class GraphAuthService : IGraphAuthService
{
    private readonly IPublicClientApplication _app;
    private readonly GraphAuthOptions _options;
    private readonly ConcurrentDictionary<string, GraphLoginSession> _pendingSessions = new();

    public GraphAuthService(IPublicClientApplication app, IOptions<GraphAuthOptions> options)
    {
        _app = app;
        _options = options.Value;
    }

    public async Task<Uri> BeginInteractiveLoginAsync(string state, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(state);

        var session = new GraphLoginSession(state);
        if (!_pendingSessions.TryAdd(state, session))
        {
            throw new InvalidOperationException($"A login session for state '{state}' is already in progress.");
        }

        // Fire-and-forget: this task's only synchronous effect the caller needs is the
        // authorizationUri, delivered via session.AuthorizationReady below. The task itself is
        // awaited later, from CompleteInteractiveLoginAsync, via session.Completion.
        _ = RunInteractiveAcquireAsync(session, cancellationToken);

        using var timeoutCts = new CancellationTokenSource(_options.AuthorizationRequestTimeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            return await session.AuthorizationReady.Task.WaitAsync(linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _pendingSessions.TryRemove(state, out _);
            throw;
        }
    }

    private async Task RunInteractiveAcquireAsync(GraphLoginSession session, CancellationToken cancellationToken)
    {
        using var lifetimeCts = new CancellationTokenSource(_options.LoginSessionTimeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lifetimeCts.Token);

        try
        {
            var webUi = new HostRedirectWebUi(session);
            var result = await _app.AcquireTokenInteractive(GraphScopes.Delegated)
                .WithCustomWebUi(webUi)
                .WithPrompt(Prompt.SelectAccount)
                .ExecuteAsync(linked.Token)
                .ConfigureAwait(false);

            session.Completion.TrySetResult(new GraphSignInOutcome(true, result.Account.Username, null));
        }
        catch (Exception ex)
        {
            session.Completion.TrySetResult(new GraphSignInOutcome(false, null, ex.Message));
        }
        finally
        {
            _pendingSessions.TryRemove(session.State, out _);
        }
    }

    public async Task<GraphSignInOutcome> CompleteInteractiveLoginAsync(
        string state,
        Uri callbackUri,
        CancellationToken cancellationToken = default)
    {
        if (!_pendingSessions.TryGetValue(state, out var session))
        {
            return new GraphSignInOutcome(false, null, "No matching sign-in session (it may have expired); start again at /auth/login.");
        }

        // Unblocks HostRedirectWebUi.AcquireAuthorizationCodeAsync, which hands this back to
        // MSAL to redeem the code (PKCE, no client secret) and complete session.Completion.
        session.CallbackReceived.TrySetResult(callbackUri);

        return await session.Completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<GraphAccountInfo?> GetCurrentAccountAsync(CancellationToken cancellationToken = default)
    {
        var account = await GetCachedAccountAsync(cancellationToken).ConfigureAwait(false);
        return account is null ? null : new GraphAccountInfo(account.Username, account.Username);
    }

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        var account = await GetCachedAccountAsync(cancellationToken).ConfigureAwait(false);
        if (account is not null)
        {
            await _app.RemoveAsync(account).ConfigureAwait(false);
        }
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        var account = await GetCachedAccountAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new GraphSignInRequiredException("No cached account; interactive sign-in (/auth/login) is required.");

        try
        {
            var result = await _app.AcquireTokenSilent(GraphScopes.Delegated, account)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);
            return result.AccessToken;
        }
        catch (MsalUiRequiredException ex)
        {
            // Refresh token expired/revoked, or CAE issued a claims challenge the caller must
            // satisfy interactively (ADR-0007). Either way, only interactive sign-in recovers.
            throw new GraphSignInRequiredException("Silent token acquisition failed; interactive sign-in (/auth/login) is required.", ex);
        }
    }

    private async Task<IAccount?> GetCachedAccountAsync(CancellationToken cancellationToken)
    {
        var accounts = await _app.GetAccountsAsync().ConfigureAwait(false);
        return accounts.FirstOrDefault();
    }
}
