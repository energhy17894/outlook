using Microsoft.Extensions.Logging.Abstractions;
using OpsIntel.AI.Extraction;
using OpsIntel.AI.Extraction.WorkItems;
using OpsIntel.Intelligence;
using OpsIntel.Persistence.Sqlite;
using OpsIntel.Platform.Abstractions;
using Xunit;

namespace OpsIntel.UnitTests;

public sealed class ExtractionWorkerTests
{
    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    public async Task MalformedPayload_IsCompletedWithoutRetry(string payload)
    {
        var queue = new SingleJobQueue(new JobLease("job-1", ExtractionWorker.WorkItemExtractionJobType, payload, 1, DateTimeOffset.UtcNow.AddMinutes(5)));
        using var db = OpsIntelDbContextFactory.Create("Data Source=:memory:");
        var worker = new ExtractionWorker(queue, new UnreachableExtractor(), new WorkItemStore(db), NullLogger<ExtractionWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);
        var outcome = await queue.Outcome.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await worker.StopAsync(CancellationToken.None);

        Assert.Equal("completed", outcome);
    }

    private sealed class SingleJobQueue(JobLease lease) : IJobQueue
    {
        private JobLease? _pending = lease;

        public TaskCompletionSource<string> Outcome { get; } = new();

        public Task<JobLease?> LeaseNextAsync(string workerId, IReadOnlyCollection<string> jobTypes, TimeSpan leaseDuration, CancellationToken cancellationToken = default)
            => Task.FromResult(Interlocked.Exchange(ref _pending, null));

        public Task CompleteAsync(string jobId, CancellationToken cancellationToken = default)
        {
            Outcome.TrySetResult("completed");
            return Task.CompletedTask;
        }

        public Task FailAsync(string jobId, string error, TimeSpan? retryAfter = null, CancellationToken cancellationToken = default)
        {
            Outcome.TrySetResult("failed");
            return Task.CompletedTask;
        }

        public Task EnqueueAsync(string jobType, string payloadJson, string idempotencyKey, DateTimeOffset? notBeforeUtc = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class UnreachableExtractor : IExtractor
    {
        public Task<ExtractionOutcome<RawWorkItem>> ExtractWorkItemsAsync(ExtractionThread thread, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("A malformed payload must never reach the extractor.");
    }
}
