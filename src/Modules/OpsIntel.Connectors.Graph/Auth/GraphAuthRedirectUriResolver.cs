using System.Globalization;

namespace OpsIntel.Connectors.Graph.Auth;

/// <summary>
/// Resolves <see cref="GraphAuthOptions.RedirectUri"/>: an explicit, configured value always
/// wins; otherwise it is derived from Host's actual Kestrel port (ADR-0003/ADR-0004: Host is
/// HTTPS-only, so the derived URI always uses the <c>https</c> scheme).
/// </summary>
public static class GraphAuthRedirectUriResolver
{
    /// <summary>Path Entra's redirect must land on; Host's <c>AuthEndpoints</c> maps this route.</summary>
    public const string CallbackPath = "/auth/callback";

    /// <summary>
    /// Returns <paramref name="configuredRedirectUri"/> unchanged if it is set, otherwise
    /// <c>https://localhost:&lt;port&gt;/auth/callback</c>.
    /// </summary>
    public static string Resolve(string? configuredRedirectUri, int port)
    {
        if (!string.IsNullOrWhiteSpace(configuredRedirectUri))
        {
            return configuredRedirectUri;
        }

        return string.Format(CultureInfo.InvariantCulture, "https://localhost:{0}{1}", port, CallbackPath);
    }
}
