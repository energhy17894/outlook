namespace OpsIntel.Platform.Abstractions;

/// <summary>A single similarity search result from <see cref="IVectorIndex.QueryAsync"/>.</summary>
public sealed record VectorMatch(string Id, float Score, IReadOnlyDictionary<string, string>? Metadata);

/// <summary>
/// A nearest-neighbour vector index over chunk embeddings (ADR-0010: SQLite + sqlite-vec is the
/// default implementation target). Used for project-assignment kNN candidates and semantic
/// search (report §3).
/// </summary>
public interface IVectorIndex
{
    /// <summary>Inserts or replaces the embedding stored under <paramref name="id"/>.</summary>
    Task UpsertAsync(
        string id,
        ReadOnlyMemory<float> embedding,
        IReadOnlyDictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the <paramref name="topK"/> nearest neighbours to <paramref name="embedding"/>.</summary>
    Task<IReadOnlyList<VectorMatch>> QueryAsync(
        ReadOnlyMemory<float> embedding,
        int topK,
        CancellationToken cancellationToken = default);

    /// <summary>Removes an embedding. Used when its source content is retention-deleted.</summary>
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
