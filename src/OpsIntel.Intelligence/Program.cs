using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpsIntel.Intelligence;
using OpsIntel.Observability;
using OpsIntel.Platform.Abstractions;

var hostBuilder = Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .AddOpsIntelObservability("OpsIntel.Intelligence")
    .ConfigureServices(services =>
    {
        // ADR-0022 / crash-restart contract: an unhandled BackgroundService exception stops
        // the whole host; the catch below exits non-zero so the Windows Service Control
        // Manager's recovery actions (restart) actually fire, instead of leaving a
        // silently-dead worker.
        services.Configure<HostOptions>(options =>
        {
            options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost;
        });

        // OpsIntel.Intelligence holds no Microsoft Graph tokens (ADR-0007/ADR-0008): it never
        // authenticates to Graph and cannot send mail; it only processes content the Host has
        // already fetched and handed off via this job queue.
        services.AddSingleton<IJobQueue, NoOpJobQueue>();
        services.AddHostedService<ExtractionWorker>();
    });

var host = hostBuilder.Build();

try
{
    await host.RunAsync();
    return 0;
}
catch (Exception ex)
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogCritical(ex, "OpsIntel.Intelligence terminated unexpectedly.");
    return 1;
}

public partial class Program
{
}
