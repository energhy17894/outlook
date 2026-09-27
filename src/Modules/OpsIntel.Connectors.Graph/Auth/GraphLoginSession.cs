namespace OpsIntel.Connectors.Graph.Auth;

/// <summary>
/// Coordinates one in-flight interactive sign-in between Host's <c>/auth/login</c> and
/// <c>/auth/callback</c> endpoints and the MSAL <c>AcquireTokenInteractive</c> call running in
/// the background, via <see cref="HostRedirectWebUi"/> (ADR-0007 spike B). See
/// <c>SPIKE-AUTH.md</c> for why this hand-off exists.
/// </summary>
internal sealed class GraphLoginSession
{
    public GraphLoginSession(string state)
    {
        State = state;
    }

    public string State { get; }

    /// <summary>
    /// Completed by <see cref="HostRedirectWebUi"/> once MSAL has built the PKCE authorization
    /// URL; awaited by <c>/auth/login</c> so it can redirect the browser to it.
    /// </summary>
    public TaskCompletionSource<Uri> AuthorizationReady { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Completed by <c>/auth/callback</c> with the full callback request URL (containing
    /// <c>code</c> and <c>state</c>); awaited by <see cref="HostRedirectWebUi"/>, which hands it
    /// back to MSAL to redeem for a token.
    /// </summary>
    public TaskCompletionSource<Uri> CallbackReceived { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// The end-to-end outcome of <c>AcquireTokenInteractive(...).ExecuteAsync()</c>, set once
    /// MSAL has redeemed the code (or failed). <c>/auth/callback</c> awaits this before replying
    /// to the browser, so it can report success/failure rather than just "redirect accepted".
    /// </summary>
    public TaskCompletionSource<GraphSignInOutcome> Completion { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}

/// <summary>The result of a completed (or failed) interactive sign-in.</summary>
public sealed record GraphSignInOutcome(bool Success, string? Username, string? ErrorMessage);

/// <summary>A cached account, as reported by <c>/auth/me</c>.</summary>
public sealed record GraphAccountInfo(string Username, string? DisplayName);
