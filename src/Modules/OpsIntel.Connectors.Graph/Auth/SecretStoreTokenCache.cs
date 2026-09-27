using Microsoft.Identity.Client;
using OpsIntel.Platform.Abstractions;

namespace OpsIntel.Connectors.Graph.Auth;

/// <summary>
/// Persists MSAL's serialized token cache (which holds the refresh token and any cached access
/// tokens) through <see cref="ISecretStore"/>, i.e. DPAPI-protected-at-rest (ADR-0007, ADR-0011).
/// The browser never sees these bytes; only Host's process reads them, via the secret store.
/// </summary>
public sealed class SecretStoreTokenCache
{
    private readonly ISecretStore _secretStore;
    private readonly string _key;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public SecretStoreTokenCache(ISecretStore secretStore, string key)
    {
        _secretStore = secretStore;
        _key = key;
    }

    /// <summary>Wires MSAL's before/after-access notifications to the secret store for <paramref name="tokenCache"/>.</summary>
    public void Attach(ITokenCache tokenCache)
    {
        tokenCache.SetBeforeAccessAsync(OnBeforeAccessAsync);
        tokenCache.SetAfterAccessAsync(OnAfterAccessAsync);
    }

    private async Task OnBeforeAccessAsync(TokenCacheNotificationArgs args)
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            var bytes = await _secretStore.RetrieveAsync(_key).ConfigureAwait(false);
            if (bytes is { } value)
            {
                args.TokenCache.DeserializeMsalV3(value.ToArray());
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task OnAfterAccessAsync(TokenCacheNotificationArgs args)
    {
        if (!args.HasStateChanged)
        {
            return;
        }

        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            var bytes = args.TokenCache.SerializeMsalV3();
            if (bytes.Length == 0)
            {
                await _secretStore.DeleteAsync(_key).ConfigureAwait(false);
            }
            else
            {
                await _secretStore.StoreAsync(_key, bytes).ConfigureAwait(false);
            }
        }
        finally
        {
            _gate.Release();
        }
    }
}
