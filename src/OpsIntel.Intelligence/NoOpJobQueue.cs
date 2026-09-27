using OpsIntel.Platform.Abstractions;

namespace OpsIntel.Intelligence;

/// <summary>
/// Placeholder <see cref="IJobQueue"/> for the Faz 0 skeleton: never has work, so
/// <see cref="ExtractionWorker"/> has something real to poll against without depending on
/// <c>OpsIntel.Persistence.Sqlite</c> yet.
/// </summary>
/// <remarks>
/// TODO: replace with the SQLite-backed job/outbox queue (ADR-0012) once
/// <c>OpsIntel.Persistence.Sqlite</c> exposes it via DI in this service.
/// </remarks>
public sealed class NoOpJobQueue : IJobQueue
{
    public Task EnqueueAsync(
        string jobType,
        string payloadJson,
        string idempotencyKey,
        DateTimeOffset? notBeforeUtc = null,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<JobLease?> LeaseNextAsync(
        string workerId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
        => Task.FromResult<JobLease?>(null);

    public Task CompleteAsync(string jobId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task FailAsync(
        string jobId,
        string error,
        TimeSpan? retryAfter = null,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
