using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Win32;

namespace OpsIntel.Platform.Windows;

/// <summary>
/// Reads <c>HKLM\SOFTWARE\OpsIntel</c> (written by installer/Config.wxs and installer/Folders.wxs
/// at install time) and exposes it as configuration, via <see cref="OpsIntelRegistryConfigMapper"/>
/// for the actual key mapping. Windows-only: <see cref="Load"/> is a no-op (empty data) on any
/// other platform, so this can safely be added to the configuration builder unconditionally by
/// code that also runs cross-platform in tests.
/// </summary>
public sealed class OpsIntelRegistryConfigurationSource : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder) => new OpsIntelRegistryConfigurationProvider();
}

/// <summary>See <see cref="OpsIntelRegistryConfigurationSource"/>.</summary>
public sealed class OpsIntelRegistryConfigurationProvider : ConfigurationProvider
{
    private const string KeyPath = @"SOFTWARE\OpsIntel";

    public override void Load()
    {
        Data = OpsIntelRegistryConfigMapper.Map(ReadRawValues());
    }

    private static IReadOnlyDictionary<string, string?> ReadRawValues()
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        if (!OperatingSystem.IsWindows())
        {
            return values;
        }

        using var key = Registry.LocalMachine.OpenSubKey(KeyPath, writable: false);
        if (key is null)
        {
            // Not installed via the MSI (e.g. `dotnet run` in dev, or the MSI hasn't run yet) —
            // not an error, just nothing to contribute.
            return values;
        }

        foreach (var name in key.GetValueNames())
        {
            values[name] = key.GetValue(name) switch
            {
                null => null,
                int i => i.ToString(CultureInfo.InvariantCulture),
                string s => s,
                var other => other.ToString(),
            };
        }

        return values;
    }
}
