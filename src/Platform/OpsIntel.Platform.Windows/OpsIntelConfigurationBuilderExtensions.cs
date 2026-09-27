using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;

namespace OpsIntel.Platform.Windows;

/// <summary>Registers the <c>HKLM\SOFTWARE\OpsIntel</c>-backed configuration source.</summary>
public static class OpsIntelConfigurationBuilderExtensions
{
    /// <summary>
    /// Adds <see cref="OpsIntelRegistryConfigurationSource"/> at a precedence between
    /// <c>appsettings*.json</c> and environment variables: an operator's environment variable
    /// (e.g. set for a manual debugging session) still overrides what the MSI wrote to the
    /// registry, but the registry still overrides the appsettings.json defaults baked into the
    /// publish output. A no-op on any OS other than Windows.
    /// </summary>
    public static IConfigurationBuilder AddOpsIntelWindowsRegistryConfiguration(this IConfigurationBuilder builder)
    {
        if (!OperatingSystem.IsWindows())
        {
            return builder;
        }

        var source = new OpsIntelRegistryConfigurationSource();

        var environmentVariablesIndex = -1;
        for (var i = 0; i < builder.Sources.Count; i++)
        {
            if (builder.Sources[i] is EnvironmentVariablesConfigurationSource)
            {
                environmentVariablesIndex = i;
                break;
            }
        }

        if (environmentVariablesIndex >= 0)
        {
            builder.Sources.Insert(environmentVariablesIndex, source);
        }
        else
        {
            // No environment-variables source registered yet (unusual, but not this method's
            // job to add one) — appending still gives the registry higher precedence than
            // whatever is already there (appsettings.json), which is the important half of the
            // contract; it will also outrank anything added after this call.
            builder.Sources.Add(source);
        }

        return builder;
    }
}
