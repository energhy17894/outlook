using System.Text;
using Microsoft.Extensions.Options;
using OpsIntel.Platform.Windows;
using Xunit;

namespace OpsIntel.UnitTests;

public sealed class DpapiSecretStoreTests : IDisposable
{
    private readonly string _tempDirectory;

    public DpapiSecretStoreTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "opsintel-dpapi-tests-" + Guid.NewGuid());
    }

    [Fact]
    public async Task StoreAndRetrieve_RoundTrips_OnWindows()
    {
        // DPAPI (System.Security.Cryptography.ProtectedData) is Windows-only. CI also runs
        // this project on Linux (EnableWindowsTargeting), so the assertion body is skipped
        // there rather than failing the build.
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var store = new DpapiSecretStore(Options.Create(new DpapiSecretStoreOptions
        {
            StorageDirectory = _tempDirectory,
        }));

        var plaintext = Encoding.UTF8.GetBytes("graph-refresh-token");
        await store.StoreAsync("token", plaintext);

        var retrieved = await store.RetrieveAsync("token");

        Assert.NotNull(retrieved);
        Assert.Equal(plaintext, retrieved!.Value.ToArray());
    }

    [Fact]
    public async Task Delete_RemovesSecret_OnWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var store = new DpapiSecretStore(Options.Create(new DpapiSecretStoreOptions
        {
            StorageDirectory = _tempDirectory,
        }));

        await store.StoreAsync("token", Encoding.UTF8.GetBytes("value"));
        await store.DeleteAsync("token");

        Assert.Null(await store.RetrieveAsync("token"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}
