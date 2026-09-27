namespace OpsIntel.Platform.Windows;

/// <summary>
/// Pure mapping from the raw <c>HKLM\SOFTWARE\OpsIntel</c> values Config.wxs/Folders.wxs write
/// at install time (installer/Config.wxs, installer/Folders.wxs) onto the
/// <c>appsettings.json</c> configuration keys Host and Intelligence already read. Kept free of
/// any actual registry access (<see cref="Microsoft.Win32.Registry"/>) so it can be unit tested
/// on any platform; <see cref="OpsIntelRegistryConfigurationProvider"/> is the Windows-only
/// caller that supplies the raw values.
/// </summary>
public static class OpsIntelRegistryConfigMapper
{
    /// <summary>Registry value name (case-insensitive) -> configuration key it feeds.</summary>
    private static readonly IReadOnlyDictionary<string, string> KeyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Port"] = "OpsIntel:Kestrel:Port",
        ["CertThumbprint"] = "OpsIntel:Kestrel:CertificateThumbprint",
        ["TenantId"] = "OpsIntel:Graph:Auth:TenantId",
        ["ClientId"] = "OpsIntel:Graph:Auth:ClientId",
        ["AllowLan"] = "OpsIntel:Network:AllowLan",
        ["DataDir"] = "OpsIntel:Paths:DataDir",
        ["LogsDir"] = "OpsIntel:Paths:LogsDir",
        ["BlobsDir"] = "OpsIntel:Paths:BlobsDir",
        ["ConfigDir"] = "OpsIntel:Paths:ConfigDir",
        ["ModelsDir"] = "OpsIntel:Paths:ModelsDir",
        ["BackupDir"] = "OpsIntel:Paths:BackupDir",
        ["InstallDir"] = "OpsIntel:Paths:InstallDir",

        // Written by Folders.wxs/Cert.wxs for drift-detection/repair purposes only; not
        // consumed as application configuration, so deliberately NOT mapped:
        //   ProductVersion, CertProvisioned, CertRemoveOnUninstall, RemoveDataOnUninstall,
        //   FirewallRuleActive.
    };

    /// <summary>
    /// Maps raw registry value-name/value pairs to configuration key/value pairs. Unknown value
    /// names are ignored (forward-compatible with a newer MSI writing values this build does not
    /// yet know about). A <see langword="null"/> or empty value is also skipped — an
    /// unprovisioned setting (e.g. CLIENT_ID never passed to msiexec, so the MSI writes an empty
    /// string) must not shadow a real value further down the configuration chain, and must
    /// behave identically to the registry value being entirely absent.
    /// </summary>
    public static IDictionary<string, string?> Map(IReadOnlyDictionary<string, string?> registryValues)
    {
        ArgumentNullException.ThrowIfNull(registryValues);

        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, value) in registryValues)
        {
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            if (KeyMap.TryGetValue(name, out var configurationKey))
            {
                result[configurationKey] = value;
            }
        }

        return result;
    }
}
