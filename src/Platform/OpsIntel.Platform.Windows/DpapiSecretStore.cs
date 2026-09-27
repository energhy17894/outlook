using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using OpsIntel.Platform.Abstractions;

namespace OpsIntel.Platform.Windows;

/// <summary>Configures <see cref="DpapiSecretStore"/>.</summary>
public sealed class DpapiSecretStoreOptions
{
    /// <summary>
    /// The DPAPI protection scope. <see cref="DataProtectionScope.LocalMachine"/> lets any
    /// process on the machine (e.g. both Windows services) decrypt; <c>CurrentUser</c> ties
    /// secrets to the running service account. Defaults to <c>LocalMachine</c> (ADR-0007: DPAPI
    /// token vault shared by Host and Intelligence).
    /// </summary>
    public DataProtectionScope Scope { get; set; } = DataProtectionScope.LocalMachine;

    /// <summary>Directory secrets are persisted to, one file per key.</summary>
    public string StorageDirectory { get; set; } = "secrets";

    /// <summary>
    /// Optional additional entropy mixed into every DPAPI call. Leave <c>null</c> to rely on
    /// scope alone; set it to further scope secrets to this application.
    /// </summary>
    public byte[]? Entropy { get; set; }
}

/// <summary>
/// <see cref="ISecretStore"/> backed by Windows Data Protection API (DPAPI). Each secret is
/// stored as an individually DPAPI-protected file so an individual key can be crypto-shredded
/// by deleting its file (ADR-0011).
/// </summary>
public sealed class DpapiSecretStore : ISecretStore
{
    private readonly DpapiSecretStoreOptions _options;

    public DpapiSecretStore(IOptions<DpapiSecretStoreOptions> options)
    {
        _options = options.Value;
        Directory.CreateDirectory(_options.StorageDirectory);
    }

    public async Task StoreAsync(string key, ReadOnlyMemory<byte> plaintext, CancellationToken cancellationToken = default)
    {
        var protectedBytes = ProtectedData.Protect(plaintext.ToArray(), _options.Entropy, _options.Scope);
        await File.WriteAllBytesAsync(PathFor(key), protectedBytes, cancellationToken).ConfigureAwait(false);
    }

    public async Task<ReadOnlyMemory<byte>?> RetrieveAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = PathFor(key);
        if (!File.Exists(path))
        {
            return null;
        }

        var protectedBytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
        return ProtectedData.Unprotect(protectedBytes, _options.Entropy, _options.Scope);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = PathFor(key);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private string PathFor(string key)
    {
        var safeName = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(key)));
        return Path.Combine(_options.StorageDirectory, safeName + ".dpapi");
    }
}
