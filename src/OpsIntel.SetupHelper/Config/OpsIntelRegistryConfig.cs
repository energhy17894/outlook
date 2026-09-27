using Microsoft.Win32;

namespace OpsIntel.SetupHelper.Config;

/// <summary>
/// Reads/writes the small slice of <c>HKLM\SOFTWARE\OpsIntel</c> this tool owns (Config.wxs
/// authors the rest at install time; this only ever touches <c>CertThumbprint</c> and reads
/// <c>DataDir</c> for <c>db backup</c>'s default source path). Windows-only: the registry does
/// not exist on any other platform, so every call here must be guarded by
/// <see cref="OperatingSystem.IsWindows"/> at the call site.
/// </summary>
public static class OpsIntelRegistryConfig
{
    private const string KeyPath = @"SOFTWARE\OpsIntel";

    public static string? GetCertThumbprint()
    {
        RequireWindows();
        using var key = Registry.LocalMachine.OpenSubKey(KeyPath, writable: false);
        return key?.GetValue("CertThumbprint") as string;
    }

    public static void SetCertThumbprint(string thumbprint)
    {
        RequireWindows();
        using var key = Registry.LocalMachine.CreateSubKey(KeyPath, writable: true);
        key.SetValue("CertThumbprint", thumbprint, RegistryValueKind.String);
    }

    public static void ClearCertThumbprint()
    {
        RequireWindows();
        using var key = Registry.LocalMachine.OpenSubKey(KeyPath, writable: true);
        key?.DeleteValue("CertThumbprint", throwOnMissingValue: false);
    }

    public static string? GetDataDir()
    {
        RequireWindows();
        using var key = Registry.LocalMachine.OpenSubKey(KeyPath, writable: false);
        return key?.GetValue("DataDir") as string;
    }

    private static void RequireWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("The registry is only available on Windows.");
        }
    }
}
