using OpsIntel.Platform.Windows;
using Xunit;

namespace OpsIntel.UnitTests;

public sealed class OpsIntelRegistryConfigMapperTests
{
    [Theory]
    [InlineData("Port", "6501", "OpsIntel:Kestrel:Port")]
    [InlineData("CertThumbprint", "ABCDEF0123456789", "OpsIntel:Kestrel:CertificateThumbprint")]
    [InlineData("TenantId", "contoso.onmicrosoft.com", "OpsIntel:Graph:Auth:TenantId")]
    [InlineData("ClientId", "11111111-1111-1111-1111-111111111111", "OpsIntel:Graph:Auth:ClientId")]
    [InlineData("AllowLan", "1", "OpsIntel:Network:AllowLan")]
    [InlineData("DataDir", @"C:\ProgramData\OpsIntel\data", "OpsIntel:Paths:DataDir")]
    [InlineData("LogsDir", @"C:\ProgramData\OpsIntel\logs", "OpsIntel:Paths:LogsDir")]
    [InlineData("BlobsDir", @"C:\ProgramData\OpsIntel\blobs", "OpsIntel:Paths:BlobsDir")]
    [InlineData("ConfigDir", @"C:\ProgramData\OpsIntel\config", "OpsIntel:Paths:ConfigDir")]
    [InlineData("ModelsDir", @"C:\ProgramData\OpsIntel\models", "OpsIntel:Paths:ModelsDir")]
    [InlineData("BackupDir", @"C:\ProgramData\OpsIntel\backup", "OpsIntel:Paths:BackupDir")]
    [InlineData("InstallDir", @"C:\Program Files\OpsIntel", "OpsIntel:Paths:InstallDir")]
    public void Map_TranslatesKnownRegistryValue_ToExpectedConfigurationKey(string registryName, string value, string expectedConfigurationKey)
    {
        var result = OpsIntelRegistryConfigMapper.Map(new Dictionary<string, string?> { [registryName] = value });

        var entry = Assert.Single(result);
        Assert.Equal(expectedConfigurationKey, entry.Key);
        Assert.Equal(value, entry.Value);
    }

    [Fact]
    public void Map_IsCaseInsensitiveOnRegistryValueName()
    {
        var result = OpsIntelRegistryConfigMapper.Map(new Dictionary<string, string?> { ["port"] = "7000" });

        Assert.Equal("7000", result["OpsIntel:Kestrel:Port"]);
    }

    [Fact]
    public void Map_SkipsUnknownRegistryValues()
    {
        var result = OpsIntelRegistryConfigMapper.Map(new Dictionary<string, string?>
        {
            ["ProductVersion"] = "2026.9.1",
            ["CertProvisioned"] = "1",
            ["FirewallRuleActive"] = "1",
        });

        Assert.Empty(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Map_SkipsEmptyOrNullValues_SoAnUnprovisionedSettingDoesNotShadowDefaults(string? value)
    {
        // MSI CLIENT_ID/TENANT_ID default to "" when no property is passed to msiexec, and
        // Config.wxs writes that empty string to the registry regardless — this must behave the
        // same as the registry key not existing at all (empty ClientId means "not configured",
        // not "override appsettings.json's ClientId with an empty string").
        var result = OpsIntelRegistryConfigMapper.Map(new Dictionary<string, string?> { ["ClientId"] = value });

        Assert.Empty(result);
    }

    [Fact]
    public void Map_TranslatesMultipleValuesAtOnce()
    {
        var result = OpsIntelRegistryConfigMapper.Map(new Dictionary<string, string?>
        {
            ["Port"] = "6500",
            ["AllowLan"] = "0",
            ["ClientId"] = "",
        });

        Assert.Equal(2, result.Count);
        Assert.Equal("6500", result["OpsIntel:Kestrel:Port"]);
        Assert.Equal("0", result["OpsIntel:Network:AllowLan"]);
        Assert.False(result.ContainsKey("OpsIntel:Graph:Auth:ClientId"));
    }

    [Fact]
    public void Map_EmptyInput_ProducesEmptyOutput()
    {
        var result = OpsIntelRegistryConfigMapper.Map(new Dictionary<string, string?>());

        Assert.Empty(result);
    }
}
