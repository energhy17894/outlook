using OpsIntel.Platform.Abstractions;

namespace OpsIntel.Intelligence;

/// <summary>
/// Placeholder for the extraction pipeline's job loop. Polls <see cref="IJobQueue"/> for
/// extraction jobs (Faz 0: always empty via <see cref="NoOpJobQueue"/>) and will eventually run
/// the quarantined extractor (report §4: "Karantinaya alınmış çıkarıcı" — no tools, schema-only
/// JSON output) against leased jobs.
/// </summary>
/// <remarks>
/// OpsIntel.Intelligence holds no Microsoft Graph tokens (ADR-0007/ADR-0008): it only ever sees
/// content the Host has already fetched and handed off via the job queue / blob store, never
/// talks to Graph itself, and cannot send mail.
/// </remarks>
public sealed class ExtractionWorker : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);

    private readonly IJobQueue _jobQueue;
    private readonly ILogger<ExtractionWorker> _logger;

    public ExtractionWorker(IJobQueue jobQueue, ILogger<ExtractionWorker> logger)
    {
        _jobQueue = jobQueue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExtractionWorker starting poll loop.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var lease = await _jobQueue.LeaseNextAsync(
                workerId: Environment.MachineName,
                leaseDuration: LeaseDuration,
                cancellationToken: stoppingToken);

            if (lease is null)
            {
                await Task.Delay(PollInterval, stoppingToken);
                continue;
            }

            // TODO: dispatch lease.JobType to the extraction pipeline (OpsIntel.AI.Extraction).
            _logger.LogInformation("Leased job {JobId} of type {JobType}.", lease.JobId, lease.JobType);
            await _jobQueue.CompleteAsync(lease.JobId, stoppingToken);
        }
    }
}
