namespace OpsIntel.Platform.Abstractions;

/// <summary>
/// Content-addressed storage for raw item bodies, attachments and other large blobs (ADR-0010).
/// Implementations are expected to key blobs by their SHA-256 content hash so identical content
/// is stored once.
/// </summary>
public interface IBlobStore
{
    /// <summary>Writes <paramref name="content"/> and returns its content-addressed blob path.</summary>
    Task<string> PutAsync(Stream content, CancellationToken cancellationToken = default);

    /// <summary>Opens a blob for reading. Throws if <paramref name="blobPath"/> does not exist.</summary>
    Task<Stream> OpenReadAsync(string blobPath, CancellationToken cancellationToken = default);

    /// <summary>Returns whether a blob exists at <paramref name="blobPath"/>.</summary>
    Task<bool> ExistsAsync(string blobPath, CancellationToken cancellationToken = default);

    /// <summary>Deletes a blob. Used for retention enforcement (report §3, "Saklama").</summary>
    Task DeleteAsync(string blobPath, CancellationToken cancellationToken = default);
}
