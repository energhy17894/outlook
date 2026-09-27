using OpsIntel.Persistence.Sqlite;
using Xunit;

namespace OpsIntel.SetupHelper.Tests;

/// <summary>
/// Exercises <see cref="SqliteJobQueue"/> against a real (temp-file) SQLite database, since the
/// atomic-lease behavior under test depends on SQLite's actual single-writer locking, which an
/// in-memory fake could not represent faithfully (ADR-0012).
/// </summary>
public sealed class SqliteJobQueueTests : IDisposable
{
    private readonly string _dbPath;
    private readonly OpsIntelDbContext _dbContext;
    private readonly TestTimeProvider _time;
    private readonly SqliteJobQueue _queue;

    public SqliteJobQueueTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"opsintel-jobqueue-tests-{Guid.NewGuid():N}.db");
        _dbContext = OpsIntelDbContextFactory.Create($"Data Source={_dbPath}");
        _time = new TestTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        _queue = new SqliteJobQueue(_dbContext, _time);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        File.Delete(_dbPath);
        var wal = _dbPath + "-wal";
        var shm = _dbPath + "-shm";
        if (File.Exists(wal)) File.Delete(wal);
        if (File.Exists(shm)) File.Delete(shm);
    }

    [Fact]
    public async Task EnqueueAsync_SameIdempotencyKeyTwice_OnlyCreatesOneJob()
    {
        await _queue.EnqueueAsync("extract", "{}", "idem-1");
        await _queue.EnqueueAsync("extract", "{}", "idem-1");

        var first = await _queue.LeaseNextAsync("worker-a", TimeSpan.FromMinutes(5));
        Assert.NotNull(first);
        await _queue.CompleteAsync(first!.JobId);

        var second = await _queue.LeaseNextAsync("worker-a", TimeSpan.FromMinutes(5));
        Assert.Null(second);
    }

    [Fact]
    public async Task EnqueueAsync_DifferentIdempotencyKeys_CreatesSeparateJobs()
    {
        await _queue.EnqueueAsync("extract", "{}", "idem-a");
        await _queue.EnqueueAsync("extract", "{}", "idem-b");

        var first = await _queue.LeaseNextAsync("worker-a", TimeSpan.FromMinutes(5));
        var second = await _queue.LeaseNextAsync("worker-a", TimeSpan.FromMinutes(5));

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.NotEqual(first!.JobId, second!.JobId);
    }

    [Fact]
    public async Task LeaseNextAsync_ReturnsNull_WhenNothingIsRunnable()
    {
        var lease = await _queue.LeaseNextAsync("worker-a", TimeSpan.FromMinutes(5));
        Assert.Null(lease);
    }

    [Fact]
    public async Task LeaseNextAsync_DoesNotLeaseAgain_UntilLeaseExpires()
    {
        await _queue.EnqueueAsync("extract", "{}", "idem-1");

        var leased = await _queue.LeaseNextAsync("worker-a", TimeSpan.FromMinutes(5));
        Assert.NotNull(leased);

        // A second worker racing immediately after must not see the same job again — lease
        // exclusivity while the lease is still valid.
        var contested = await _queue.LeaseNextAsync("worker-b", TimeSpan.FromMinutes(5));
        Assert.Null(contested);
    }

    [Fact]
    public async Task LeaseNextAsync_ReLeasesJob_AfterLeaseExpires()
    {
        await _queue.EnqueueAsync("extract", "{}", "idem-1");

        var first = await _queue.LeaseNextAsync("worker-a", TimeSpan.FromMinutes(1));
        Assert.NotNull(first);

        // Simulate worker-a crashing: nobody completes/fails the job, and the lease elapses.
        _time.Advance(TimeSpan.FromMinutes(2));

        var reLeased = await _queue.LeaseNextAsync("worker-b", TimeSpan.FromMinutes(1));
        Assert.NotNull(reLeased);
        Assert.Equal(first!.JobId, reLeased!.JobId);

        // Re-leasing counts as another delivery attempt (at-least-once execution, ADR-0012).
        Assert.Equal(first.Attempts + 1, reLeased.Attempts);
    }

    [Fact]
    public async Task CompleteAsync_MarksJobDone_SoItIsNeverLeasedAgain()
    {
        await _queue.EnqueueAsync("extract", "{}", "idem-1");
        var leased = await _queue.LeaseNextAsync("worker-a", TimeSpan.FromMinutes(5));
        Assert.NotNull(leased);

        await _queue.CompleteAsync(leased!.JobId);

        _time.Advance(TimeSpan.FromHours(1));
        var again = await _queue.LeaseNextAsync("worker-b", TimeSpan.FromMinutes(5));
        Assert.Null(again);
    }

    [Fact]
    public async Task FailAsync_SchedulesRetry_NotBeforeBackoffElapses()
    {
        await _queue.EnqueueAsync("extract", "{}", "idem-1");
        var leased = await _queue.LeaseNextAsync("worker-a", TimeSpan.FromMinutes(5));
        Assert.NotNull(leased);

        await _queue.FailAsync(leased!.JobId, "boom");

        // Immediately after failing, the job should not be runnable again (still within backoff).
        var tooSoon = await _queue.LeaseNextAsync("worker-b", TimeSpan.FromMinutes(5));
        Assert.Null(tooSoon);

        _time.Advance(TimeSpan.FromHours(2));
        var retried = await _queue.LeaseNextAsync("worker-b", TimeSpan.FromMinutes(5));
        Assert.NotNull(retried);
        Assert.Equal(leased.JobId, retried!.JobId);
    }

    [Fact]
    public async Task FailAsync_RespectsExplicitRetryAfter()
    {
        await _queue.EnqueueAsync("extract", "{}", "idem-1");
        var leased = await _queue.LeaseNextAsync("worker-a", TimeSpan.FromMinutes(5));
        Assert.NotNull(leased);

        await _queue.FailAsync(leased!.JobId, "boom", retryAfter: TimeSpan.FromMinutes(10));

        _time.Advance(TimeSpan.FromMinutes(5));
        Assert.Null(await _queue.LeaseNextAsync("worker-b", TimeSpan.FromMinutes(5)));

        _time.Advance(TimeSpan.FromMinutes(6));
        Assert.NotNull(await _queue.LeaseNextAsync("worker-b", TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public async Task FailAsync_BecomesDeadLetter_AfterMaxAttempts()
    {
        await _queue.EnqueueAsync("extract", "{}", "idem-1");

        string jobId = null!;
        for (var i = 0; i < SqliteJobQueue.MaxAttempts; i++)
        {
            var leased = await _queue.LeaseNextAsync("worker-a", TimeSpan.FromMinutes(1));
            Assert.NotNull(leased);
            jobId = leased!.JobId;
            await _queue.FailAsync(jobId, $"attempt {i} failed", retryAfter: TimeSpan.Zero);
        }

        // Every retry budget spent: the job must no longer be runnable, even with time advanced
        // far into the future.
        _time.Advance(TimeSpan.FromDays(365));
        var afterExhaustion = await _queue.LeaseNextAsync("worker-b", TimeSpan.FromMinutes(1));
        Assert.Null(afterExhaustion);

        var state = await ReadJobStateAsync(jobId);
        Assert.Equal(JobStates.Failed, state);
    }

    [Fact]
    public async Task ComputeBackoff_GrowsExponentially_ThenCaps()
    {
        var backoff1 = SqliteJobQueue.ComputeBackoff(1);
        var backoff2 = SqliteJobQueue.ComputeBackoff(2);
        var backoff3 = SqliteJobQueue.ComputeBackoff(3);
        var backoffLarge = SqliteJobQueue.ComputeBackoff(50);

        Assert.True(backoff2 > backoff1);
        Assert.True(backoff3 > backoff2);
        Assert.True(backoffLarge <= TimeSpan.FromHours(1));
    }

    private async Task<string> ReadJobStateAsync(string jobId)
    {
        var id = long.Parse(jobId, System.Globalization.CultureInfo.InvariantCulture);
        var row = await _dbContext.Jobs.FindAsync(id);
        Assert.NotNull(row);
        return row!.State;
    }
}
