using Microsoft.Identity.Client.Extensibility;

namespace OpsIntel.Connectors.Graph.Auth;

/// <summary>
/// MSAL.NET's <see cref="ICustomWebUi"/> extensibility point, used instead of MSAL's own
/// embedded/system browser so that Host itself (a BFF) can drive the redirect and code capture
/// for a *public* client (ADR-0007 spike B). MSAL still does everything security-sensitive
/// itself: it generates the PKCE <c>code_verifier</c>/<c>code_challenge</c>, builds the
/// authorization URL, and — once this class hands back the redirected callback URL — performs
/// the authorization-code-for-token exchange with no client secret. This class only relays two
/// URLs across the HTTP request boundary; see <c>SPIKE-AUTH.md</c> for the full writeup of why
/// this is the correct extensibility point (as opposed to
/// <c>IConfidentialClientApplication.AcquireTokenByAuthorizationCode</c>, which is
/// confidential-client only and cannot be used here).
/// </summary>
internal sealed class HostRedirectWebUi : ICustomWebUi
{
    private readonly GraphLoginSession _session;

    public HostRedirectWebUi(GraphLoginSession session)
    {
        _session = session;
    }

    public async Task<Uri> AcquireAuthorizationCodeAsync(
        Uri authorizationUri,
        Uri redirectUri,
        CancellationToken cancellationToken)
    {
        // Unblocks /auth/login, which is awaiting this to redirect the browser to Entra.
        _session.AuthorizationReady.TrySetResult(authorizationUri);

        // Blocks until /auth/callback delivers the browser's redirected-back request URL.
        using var registration = cancellationToken.Register(
            () => _session.CallbackReceived.TrySetCanceled(cancellationToken));
        return await _session.CallbackReceived.Task.ConfigureAwait(false);
    }
}
