using Microsoft.Kiota.Abstractions.Authentication;
using OpsIntel.Connectors.Graph.Auth;

namespace OpsIntel.Connectors.Graph.Http;

/// <summary>
/// Bridges <see cref="IGraphAuthService"/>'s silent token acquisition into the Graph SDK's
/// Kiota-based authentication pipeline (<see cref="BaseBearerTokenAuthenticationProvider"/>).
/// </summary>
public sealed class GraphAccessTokenProvider : IAccessTokenProvider
{
    private readonly IGraphAuthService _authService;

    public GraphAccessTokenProvider(IGraphAuthService authService)
    {
        _authService = authService;
    }

    public Task<string> GetAuthorizationTokenAsync(
        Uri uri,
        Dictionary<string, object>? additionalAuthenticationContext = null,
        CancellationToken cancellationToken = default) =>
        _authService.GetAccessTokenAsync(cancellationToken);

    public AllowedHostsValidator AllowedHostsValidator { get; } = new(["graph.microsoft.com"]);
}
