using System.Net.Http.Headers;
using OpsIntel.Connectors.Graph.Auth;

namespace OpsIntel.Connectors.Graph.Http;

/// <summary>
/// Attaches the signed-in user's delegated Graph token (ADR-0007) to requests that bypass the
/// Graph SDK's Kiota auth provider — the "thin raw HTTP path" of <c>GraphMailChangeFeed</c> and
/// <c>DraftWriter</c> (ADR-0009). An Authorization header already set (by
/// <see cref="GraphAccessTokenProvider"/> on <c>GraphServiceClient</c> calls) is left as is.
/// </summary>
public sealed class GraphBearerTokenHandler : DelegatingHandler
{
    private readonly IGraphAuthService _authService;

    public GraphBearerTokenHandler(IGraphAuthService authService)
    {
        _authService = authService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization is null)
        {
            var token = await _authService.GetAccessTokenAsync(cancellationToken).ConfigureAwait(false);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
