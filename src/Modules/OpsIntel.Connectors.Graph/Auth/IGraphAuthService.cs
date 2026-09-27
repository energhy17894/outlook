namespace OpsIntel.Connectors.Graph.Auth;

/// <summary>
/// The Host-facing surface of the delegated auth BFF (ADR-0007). Host's <c>Auth/</c> minimal-API
/// endpoints (<c>/auth/login</c>, <c>/auth/callback</c>, <c>/auth/logout</c>, <c>/auth/me</c>)
/// are thin wrappers over this interface; all MSAL/PKCE mechanics live in this connector module.
/// </summary>
public interface IGraphAuthService
{
    /// <summary>
    /// Starts an interactive sign-in and returns the Entra authorization URL the browser must be
    /// redirected to (PKCE challenge already attached by MSAL). Call
    /// <see cref="CompleteInteractiveLoginAsync"/> with the same <paramref name="state"/> once
    /// the browser is redirected back.
    /// </summary>
    Task<Uri> BeginInteractiveLoginAsync(string state, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completes a sign-in previously started with <see cref="BeginInteractiveLoginAsync"/>,
    /// given the full callback request URL (containing <c>code</c> and <c>state</c>).
    /// </summary>
    Task<GraphSignInOutcome> CompleteInteractiveLoginAsync(
        string state,
        Uri callbackUri,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the signed-in account, if any, without prompting.</summary>
    Task<GraphAccountInfo?> GetCurrentAccountAsync(CancellationToken cancellationToken = default);

    /// <summary>Signs out: removes the cached account and its tokens.</summary>
    Task SignOutAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a Graph access token for the cached account, silently. Throws
    /// <see cref="GraphSignInRequiredException"/> if interactive sign-in is required (no cached
    /// account, or the refresh token/consent was revoked) — Host's Graph-calling code should
    /// surface this as "sign in again" rather than retry.
    /// </summary>
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}

/// <summary>Thrown when a Graph call needs a token but only interactive sign-in can produce one.</summary>
public sealed class GraphSignInRequiredException(string message, Exception? inner = null)
    : Exception(message, inner);
