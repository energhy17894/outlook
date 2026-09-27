namespace OpsIntel.Platform.Abstractions;

/// <summary>A single full-text search result from <see cref="ISearchIndex.SearchAsync"/>.</summary>
public sealed record SearchHit(string DocumentId, double Rank, string? Snippet);

/// <summary>
/// Full-text search over normalized chunk text (ADR-0010: SQLite FTS5 trigram tokenizer for
/// Turkish/English content).
/// </summary>
public interface ISearchIndex
{
    /// <summary>Indexes (or re-indexes) a document's text under <paramref name="documentId"/>.</summary>
    Task IndexAsync(
        string documentId,
        string text,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>Runs a full-text query and returns up to <paramref name="limit"/> ranked hits.</summary>
    Task<IReadOnlyList<SearchHit>> SearchAsync(
        string query,
        int limit = 20,
        CancellationToken cancellationToken = default);

    /// <summary>Removes a document from the index.</summary>
    Task DeleteAsync(string documentId, CancellationToken cancellationToken = default);
}
