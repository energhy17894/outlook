using System.Text.Json;
using OpsIntel.AI.Extraction;
using OpsIntel.Platform.Abstractions;

namespace OpsIntel.Intelligence;

/// <summary>
/// The extraction pipeline's job loop. Polls <see cref="IJobQueue"/> for extraction jobs
/// (Faz 0: always empty via <see cref="NoOpJobQueue"/>) and runs the quarantined
/// <see cref="IExtractor"/> (report §4: "Karantinaya alınmış çıkarıcı" — no tools,
/// schema-only JSON output) against leased jobs.
/// </summary>
/// <remarks>
/// OpsIntel.Intelligence holds no Microsoft Graph tokens (ADR-0007/ADR-0008): it only ever sees
/// content the Host has already fetched and handed off via the job queue / blob store, never
/// talks to Graph itself, and cannot send mail.
/// </remarks>
public sealed class ExtractionWorker : BackgroundService
{
    /// <summary>Job type dispatched to <see cref="IExtractor.ExtractWorkItemsAsync"/>; payload is a JSON-serialized <see cref="ExtractionThread"/>.</summary>
    public const string WorkItemExtractionJobType = "work_items_extraction";

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(5);

    private readonly IJobQueue _jobQueue;
    private readonly IExtractor _extractor;
    private readonly ILogger<ExtractionWorker> _logger;

    public ExtractionWorker(IJobQueue jobQueue, IExtractor extractor, ILogger<ExtractionWorker> logger)
    {
        _jobQueue = jobQueue;
        _extractor = extractor;
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

            _logger.LogInformation("Leased job {JobId} of type {JobType}.", lease.JobId, lease.JobType);

            if (lease.JobType == WorkItemExtractionJobType)
            {
                if (await TryDispatchWorkItemExtractionAsync(lease, stoppingToken))
                {
                    await _jobQueue.CompleteAsync(lease.JobId, stoppingToken);
                }

                continue;
            }

            _logger.LogWarning("Unrecognized job type {JobType}; completing without dispatch.", lease.JobType);
            await _jobQueue.CompleteAsync(lease.JobId, stoppingToken);
        }
    }

    /// <summary>Returns true if the job completed successfully (and should be marked <see cref="IJobQueue.CompleteAsync"/>); false if it was failed via <see cref="IJobQueue.FailAsync"/>.</summary>
    private async Task<bool> TryDispatchWorkItemExtractionAsync(JobLease lease, CancellationToken cancellationToken)
    {
        try
        {
            var thread = JsonSerializer.Deserialize<ExtractionThread>(lease.PayloadJson)
                ?? throw new InvalidOperationException("Job payload deserialized to null.");

            var outcome = await _extractor.ExtractWorkItemsAsync(thread, cancellationToken);

            _logger.LogInformation(
                "work_items extraction for thread {ThreadId}: {Kept} item(s) kept, {Dropped} dropped (no verifiable evidence).",
                thread.ThreadId,
                outcome.Items.Count,
                outcome.DroppedForNoVerifiableEvidence);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "work_items extraction failed for job {JobId}.", lease.JobId);
            await _jobQueue.FailAsync(lease.JobId, ex.Message, cancellationToken: cancellationToken);
            return false;
        }
    }
}
