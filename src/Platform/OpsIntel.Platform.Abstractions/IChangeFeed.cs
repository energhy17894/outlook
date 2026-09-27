namespace OpsIntel.Platform.Abstractions;

/// <summary>One changed (or removed) item reported by a <see cref="IChangeFeed"/> poll.</summary>
public sealed record ChangeFeedItem(string ItemId, bool IsRemoved, string? RawPayloadJson);

/// <summary>The result of a single <see cref="IChangeFeed.PollAsync"/> call.</summary>
public sealed record ChangeFeedPage(
    IReadOnlyList<ChangeFeedItem> Items,
    string? NextDeltaLink,
    bool ResyncRequired);

/// <summary>
/// Delta-query based change detection (ADR-0009: polling only; no webhook, no public
/// endpoint). Implementations wrap Microsoft Graph delta queries for mail, calendar and drive
/// sources; a <c>410 Gone</c> / <c>syncStateNotFound</c> response surfaces as
/// <see cref="ChangeFeedPage.ResyncRequired"/> so the caller can do a full resync and
/// reconcile deletions.
/// </summary>
public interface IChangeFeed
{
    /// <summary>
    /// Polls for changes since <paramref name="deltaLink"/> (an opaque, source-specific
    /// continuation token; <c>null</c> starts a full initial sync).
    /// </summary>
    Task<ChangeFeedPage> PollAsync(
        string sourceId,
        string? deltaLink,
        CancellationToken cancellationToken = default);
}
