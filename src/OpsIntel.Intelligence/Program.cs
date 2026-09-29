using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpsIntel.AI.Extraction;
using OpsIntel.Intelligence;
using OpsIntel.Observability;
using OpsIntel.Persistence.Sqlite;
using OpsIntel.Platform.Abstractions;
using OpsIntel.Platform.Windows;

var hostBuilder = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(config =>
    {
        // MSI-provisioned config (installer/Config.wxs writes HKLM\SOFTWARE\OpsIntel): lower
        // precedence than environment variables, higher than appsettings.json. No-op on
        // non-Windows.
        if (OperatingSystem.IsWindows())
        {
            config.AddOpsIntelWindowsRegistryConfiguration();
        }
    })
    .UseWindowsService()
    .AddOpsIntelObservability("OpsIntel.Intelligence")
    .ConfigureServices((context, services) =>
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
        // ADR-0010/0012: the shared SQLite job/outbox queue under DataDir (the Host enqueues,
        // this service leases). WAL mode lets both services hold the file open concurrently.
        // ponytail: one DbContext for the process — fine for ExtractionWorker's single sequential
        // loop; switch to IDbContextFactory once more than one worker polls concurrently.
        services.AddSingleton(_ =>
        {
            // DataDir\app.db — the same file SetupHelper's `db backup` defaults to.
            var dbPath = OpsIntelPaths.ResolveDirectory(context.Configuration, "DataDir", "app.db");
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
            return OpsIntelDbContextFactory.Create($"Data Source={dbPath}");
        });
        services.AddSingleton<IJobQueue>(sp => new SqliteJobQueue(sp.GetRequiredService<OpsIntelDbContext>()));
        services.AddSingleton<WorkItemStore>();

        // Quarantined AI extraction pipeline (ADR-0013/ADR-0015/ADR-0019): no tools, no
        // Graph tokens — see OpsIntel.AI.Extraction.ServiceCollectionExtensions.
        services.AddOpsIntelAiExtraction(context.Configuration);

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
