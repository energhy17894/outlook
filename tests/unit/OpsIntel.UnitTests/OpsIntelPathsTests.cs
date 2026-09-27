using Microsoft.Extensions.Configuration;
using OpsIntel.Platform.Abstractions;
using Xunit;

namespace OpsIntel.UnitTests;

public sealed class OpsIntelPathsTests
{
    private static IConfiguration BuildConfiguration(params (string Key, string Value)[] entries)
    {
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(entries.Select(e => new KeyValuePair<string, string?>(e.Key, e.Value)));
        return builder.Build();
    }

    [Fact]
    public void ResolveDirectory_RootedValue_IsReturnedUnchanged()
    {
        // An absolute path in whatever form THIS OS considers rooted (this test project also
        // runs on ubuntu-latest in CI, where a bare "D:\..." string is not a rooted path).
        var rooted = OperatingSystem.IsWindows() ? @"D:\Somewhere\Else\logs" : "/somewhere/else/logs";
        var configuration = BuildConfiguration(("OpsIntel:Paths:LogsDir", @"C:\ProgramData\OpsIntel\logs"));

        var resolved = OpsIntelPaths.ResolveDirectory(configuration, "LogsDir", rooted);

        Assert.Equal(rooted, resolved);
    }

    [Fact]
    public void ResolveDirectory_RelativeValue_WithConfiguredBase_CombinesWithRegistryBackedPath()
    {
        var configuration = BuildConfiguration(("OpsIntel:Paths:ConfigDir", @"C:\ProgramData\OpsIntel\config"));

        var resolved = OpsIntelPaths.ResolveDirectory(configuration, "ConfigDir", "secrets");

        Assert.Equal(Path.Combine(@"C:\ProgramData\OpsIntel\config", "secrets"), resolved);
    }

    [Fact]
    public void ResolveDirectory_RelativeValue_WithNoConfiguredBase_FallsBackToAppBaseDirectory()
    {
        var configuration = BuildConfiguration();

        var resolved = OpsIntelPaths.ResolveDirectory(configuration, "LogsDir", "logs");

        Assert.Equal(Path.Combine(AppContext.BaseDirectory, "logs"), resolved);
    }

    [Fact]
    public void ResolveDirectory_ConfiguredBaseIsWhitespace_FallsBackToAppBaseDirectory()
    {
        var configuration = BuildConfiguration(("OpsIntel:Paths:LogsDir", "   "));

        var resolved = OpsIntelPaths.ResolveDirectory(configuration, "LogsDir", "logs");

        Assert.Equal(Path.Combine(AppContext.BaseDirectory, "logs"), resolved);
    }
}
