namespace OpsIntel.Connectors.Graph;

/// <summary>Non-auth connector settings. Bound from <c>OpsIntel:Graph</c>.</summary>
public sealed class GraphConnectorOptions
{
    public const string ConfigurationSection = "OpsIntel:Graph";

    /// <summary>Outlook's documented per-mailbox concurrency ceiling (ADR-0009).</summary>
    public int MaxConcurrentRequestsPerMailbox { get; set; } = 4;

    /// <summary>Retries attempted for a throttled (429/503) request before giving up.</summary>
    public int MaxThrottledRetries { get; set; } = 3;
}
