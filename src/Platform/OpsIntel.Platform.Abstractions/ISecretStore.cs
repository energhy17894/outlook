namespace OpsIntel.Platform.Abstractions;

/// <summary>
/// Local, machine/user-bound secret storage (ADR-0007, ADR-0011). The Windows implementation
/// wraps DPAPI; encrypted database keys and token vaults are stored through this abstraction so
/// a "crypto-shred" (delete the wrapped key) makes the protected data unrecoverable.
/// </summary>
public interface ISecretStore
{
    /// <summary>Encrypts and persists <paramref name="plaintext"/> under <paramref name="key"/>.</summary>
    Task StoreAsync(string key, ReadOnlyMemory<byte> plaintext, CancellationToken cancellationToken = default);

    /// <summary>Retrieves and decrypts the secret stored under <paramref name="key"/>, or <c>null</c>.</summary>
    Task<ReadOnlyMemory<byte>?> RetrieveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Deletes a stored secret. Enables crypto-shred style erasure.</summary>
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
