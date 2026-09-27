using Microsoft.Extensions.Configuration;

namespace OpsIntel.Platform.Abstractions;

/// <summary>
/// Resolves the writable directories a service actually needs (secrets store, logs, blobs,
/// SQLite DB, ...) against <c>%ProgramData%\OpsIntel\...</c> when installed, instead of against
/// the process's content-root directory (<c>%ProgramFiles%\OpsIntel\...</c>) — which the Host
/// and Intelligence virtual-account service identities have no write access to (only the
/// per-machine Administrators/SYSTEM principals do; see installer/Folders.wxs's
/// <c>util:PermissionEx</c> grants, which target the ProgramData tree only).
/// </summary>
/// <remarks>
/// The <c>OpsIntel:Paths:*</c> configuration keys this reads are populated on Windows by
/// <c>OpsIntel.Platform.Windows</c>'s registry configuration provider, from the absolute paths
/// installer/Folders.wxs wrote to <c>HKLM\SOFTWARE\OpsIntel</c> at install time (DataDir,
/// LogsDir, BlobsDir, ConfigDir, ModelsDir, BackupDir). When that key is absent — local/dev runs,
/// unit tests, any non-Windows environment — <paramref name="pathsKey"/>'s directory falls back
/// to a subdirectory of <see cref="AppContext.BaseDirectory"/>, matching this project's
/// pre-existing local-dev behavior.
/// </remarks>
public static class OpsIntelPaths
{
    /// <summary>
    /// Resolves <paramref name="configuredValue"/> (e.g. <c>OpsIntel:Secrets:StorageDirectory</c>'s
    /// bound value) to an absolute, writable directory. Already-rooted values are returned
    /// unchanged (an explicit override always wins). A relative value is combined with the
    /// registry-backed base directory named by <paramref name="pathsKey"/>
    /// (<c>OpsIntel:Paths:&lt;pathsKey&gt;</c>) if one is configured, else with
    /// <see cref="AppContext.BaseDirectory"/>.
    /// </summary>
    public static string ResolveDirectory(IConfiguration configuration, string pathsKey, string configuredValue)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(pathsKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(configuredValue);

        if (Path.IsPathRooted(configuredValue))
        {
            return configuredValue;
        }

        var configuredBase = configuration[$"OpsIntel:Paths:{pathsKey}"];
        var baseDirectory = string.IsNullOrWhiteSpace(configuredBase) ? AppContext.BaseDirectory : configuredBase;
        return Path.Combine(baseDirectory, configuredValue);
    }
}
