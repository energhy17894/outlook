namespace OpsIntel.Connectors.Graph;

/// <summary>Non-auth connector settings. Bound from <c>OpsIntel:Graph</c>.</summary>
public sealed class GraphConnectorOptions
{
    public const string ConfigurationSection = "OpsIntel:Graph";

    /// <summary>Outlook's documented per-mailbox concurrency ceiling (ADR-0009).</summary>
    public int MaxConcurrentRequestsPerMailbox { get; set; } = 4;

    /// <summary>Retries attempted for a throttled (429/503) request before giving up.</summary>
    public int MaxThrottledRetries { get; set; } = 3;

    /// <summary>
    /// Mail folder IDs (or well-known names) Host's mail sync polls via delta (ADR-0009). Null or
    /// empty means just <c>inbox</c> — left null here because the configuration binder appends
    /// to, rather than replaces, a pre-populated array.
    /// </summary>
    public string[]? MailSyncFolderIds { get; set; }

    /// <summary>Delay between mail delta rounds (also the base of the error backoff).</summary>
    public TimeSpan MailSyncInterval { get; set; } = TimeSpan.FromMinutes(5);
}
