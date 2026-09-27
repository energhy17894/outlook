using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;

namespace OpsIntel.Observability;

/// <summary>Wires up Serilog + OpenTelemetry for an OpsIntel host (ADR-0021).</summary>
public static class ObservabilityExtensions
{
    /// <summary>
    /// Configures Serilog (console + rolling file sinks, content redaction, environment
    /// enrichment) and a basic OpenTelemetry tracing/metrics pipeline for <paramref name="builder"/>.
    /// </summary>
    public static IHostBuilder AddOpsIntelObservability(this IHostBuilder builder, string serviceName)
    {
        builder.UseSerilog((context, services, configuration) =>
        {
            configuration
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentName()
                .Enrich.With<RedactionEnricher>()
                .WriteTo.Console()
                .WriteTo.File(
                    Path.Combine(AppContext.BaseDirectory, "logs", $"{serviceName}-.log"),
                    rollingInterval: RollingInterval.Day,
                    restrictedToMinimumLevel: LogEventLevel.Information);
        });

        builder.ConfigureServices(services =>
        {
            services.AddOpenTelemetry()
                .ConfigureResource(resource => resource.AddService(serviceName))
                .WithTracing(tracing => tracing.AddSource(serviceName).AddConsoleExporter())
                .WithMetrics(metrics => metrics.AddMeter(serviceName).AddConsoleExporter());
        });

        return builder;
    }
}
