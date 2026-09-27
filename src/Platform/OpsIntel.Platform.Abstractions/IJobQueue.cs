namespace OpsIntel.Platform.Abstractions;

/// <summary>
/// A leased job handed out by <see cref="IJobQueue.LeaseNextAsync"/>.
/// </summary>
public sealed record JobLease(
    string JobId,
    string JobType,
    string PayloadJson,
    int Attempts,
    DateTimeOffset LeasedUntilUtc);

/// <summary>
/// The DB-backed job/outbox queue (ADR-0012: DB job/outbox + Channels + Quartz.NET; no
/// Temporal/Dapr/Hangfire). Idempotency keys make re-enqueue of the same logical work safe.
/// </summary>
public interface IJobQueue
{
    /// <summary>
    /// Enqueues a job. If a job with the same <paramref name="idempotencyKey"/> already exists,
    /// implementations must treat this as a no-op rather than creating a duplicate.
    /// </summary>
    Task EnqueueAsync(
        string jobType,
        string payloadJson,
        string idempotencyKey,
        DateTimeOffset? notBeforeUtc = null,
        CancellationToken cancellationToken = default);

    /// <summary>Atomically leases the next runnable job, or <c>null</c> if none is due.</summary>
    Task<JobLease?> LeaseNextAsync(
        string workerId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);

    /// <summary>Marks a leased job as successfully completed.</summary>
    Task CompleteAsync(string jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a leased job as failed. Implementations schedule a retry (respecting
    /// <paramref name="retryAfter"/>) up to a max-attempts threshold, after which the job
    /// becomes a visible dead-letter entry (report §3, <c>Job</c>).
    /// </summary>
    Task FailAsync(
        string jobId,
        string error,
        TimeSpan? retryAfter = null,
        CancellationToken cancellationToken = default);
}
