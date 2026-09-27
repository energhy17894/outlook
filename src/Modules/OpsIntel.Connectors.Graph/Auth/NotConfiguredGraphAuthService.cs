namespace OpsIntel.Connectors.Graph.Auth;

/// <summary>
/// Thrown by <see cref="NotConfiguredGraphAuthService"/> when Microsoft Graph sign-in is asked
/// for before <c>OpsIntel:Graph:Auth:ClientId</c> has been provisioned. Host's
/// <c>AuthEndpoints</c> catches this and returns HTTP 503, instead of the process crashing (or
/// MSAL throwing deep inside <c>PublicClientApplicationBuilder.Create(string.Empty)</c>) — see
/// the Faz 0 installer review item on starting with an empty ClientId.
/// </summary>
public sealed class GraphAuthNotConfiguredException(string message) : Exception(message);

/// <summary>
/// A no-op <see cref="IGraphAuthService"/> registered instead of the real MSAL-backed
/// implementation when <see cref="GraphAuthOptions.ClientId"/> is empty (no tenant/app
/// registration configured yet). Lets Host start and serve <c>/health/live</c> and the SPA
/// normally; only the sign-in endpoints report "not configured".
/// </summary>
internal sealed class NotConfiguredGraphAuthService : IGraphAuthService
{
    internal const string NotConfiguredMessage =
        "Microsoft Graph sign-in is not configured (OpsIntel:Graph:Auth:ClientId is empty). " +
        "Set ClientId (and TenantId) via the MSI's CLIENT_ID/TENANT_ID properties, then reinstall or restart the service, to enable sign-in.";

    public Task<Uri> BeginInteractiveLoginAsync(string state, CancellationToken cancellationToken = default)
        => throw new GraphAuthNotConfiguredException(NotConfiguredMessage);

    public Task<GraphSignInOutcome> CompleteInteractiveLoginAsync(string state, Uri callbackUri, CancellationToken cancellationToken = default)
        => throw new GraphAuthNotConfiguredException(NotConfiguredMessage);

    public Task<GraphAccountInfo?> GetCurrentAccountAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<GraphAccountInfo?>(null);

    public Task SignOutAsync(CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        => throw new GraphAuthNotConfiguredException(NotConfiguredMessage);
}
