namespace OpsIntel.Connectors.Graph.Auth;

/// <summary>
/// Configuration for the Entra public-client + PKCE BFF flow (ADR-0007). Bound from
/// <c>OpsIntel:Graph:Auth</c>.
/// </summary>
public sealed class GraphAuthOptions
{
    public const string ConfigurationSection = "OpsIntel:Graph:Auth";

    /// <summary>The public-client application (registered with no client secret) in Entra ID.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// "organizations", "common", "consumers", or a specific tenant GUID/domain. Defaults to
    /// "organizations" (work/school accounts only; no personal Microsoft accounts).
    /// </summary>
    public string TenantId { get; set; } = "organizations";

    /// <summary>
    /// The loopback redirect URI registered for this app under Entra's "Mobile and desktop
    /// applications" platform. Host's <c>/auth/callback</c> endpoint must be reachable at this
    /// exact URI (ADR-0007: Entra ignores port matching only for the bare
    /// <c>http://localhost</c> form with no path; because Host is a long-running local service
    /// rather than a short-lived per-login listener, this connector uses a fixed, registered
    /// path instead of relying on that special case).
    /// </summary>
    public string RedirectUri { get; set; } = "http://localhost:6500/auth/callback";

    /// <summary>Key this connector's MSAL token cache blob is stored under in <see cref="OpsIntel.Platform.Abstractions.ISecretStore"/>.</summary>
    public string TokenCacheSecretKey { get; set; } = "graph-msal-token-cache";

    /// <summary>How long <c>/auth/login</c> waits for Entra's authorization redirect to be built before giving up.</summary>
    public TimeSpan AuthorizationRequestTimeout { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>How long a pending login session is kept waiting for the browser to complete sign-in before it is discarded.</summary>
    public TimeSpan LoginSessionTimeout { get; set; } = TimeSpan.FromMinutes(5);

    public string Authority => $"https://login.microsoftonline.com/{TenantId}/v2.0";
}
