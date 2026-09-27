using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OpsIntel.Platform.Abstractions;

namespace OpsIntel.Persistence.Sqlite;

/// <summary>
/// The SQLite-backed durable job/outbox queue (ADR-0012). Uses the raw SQLite connection
/// underneath <see cref="OpsIntelDbContext"/> directly (rather than EF Core change tracking)
/// so the lease/complete/fail operations can be expressed as small, explicit transactions with
/// exact locking semantics: SQLite only ever has one writer at a time, and <c>BEGIN IMMEDIATE</c>
/// acquires that writer lock up front, so two workers racing for the same job serialize on the
/// database rather than reading-then-writing a stale row.
/// </summary>
public sealed class SqliteJobQueue : IJobQueue
{
    /// <summary>
    /// Max delivery attempts before a job becomes a terminal, visible dead letter
    /// (<see cref="JobStates.Failed"/>) instead of being retried again (ADR-0012).
    /// </summary>
    public const int MaxAttempts = 5;

    /// <summary>Base delay for exponential backoff when a caller doesn't specify <c>retryAfter</c>.</summary>
    private static readonly TimeSpan BaseBackoff = TimeSpan.FromSeconds(30);

    /// <summary>Ceiling for exponential backoff.</summary>
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromHours(1);

    private readonly OpsIntelDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public SqliteJobQueue(OpsIntelDbContext dbContext, TimeProvider? timeProvider = null)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task EnqueueAsync(
        string jobType,
        string payloadJson,
        string idempotencyKey,
        DateTimeOffset? notBeforeUtc = null,
        CancellationToken cancellationToken = default)
    {
        var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var now = _timeProvider.GetUtcNow();

        // ON CONFLICT DO NOTHING makes re-enqueueing the same idempotency key a true no-op: no
        // exception, no duplicate row, whatever state the existing job is already in is left
        // untouched.
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO jobs (type, payload, idempotency_key, state, attempts, not_before_utc, created_at_utc, updated_at_utc)
            VALUES ($type, $payload, $idempotencyKey, $state, 0, $notBefore, $now, $now)
            ON CONFLICT(idempotency_key) DO NOTHING;
            """;
        command.Parameters.AddWithValue("$type", jobType);
        command.Parameters.AddWithValue("$payload", payloadJson);
        command.Parameters.AddWithValue("$idempotencyKey", idempotencyKey);
        command.Parameters.AddWithValue("$state", JobStates.Pending);
        command.Parameters.AddWithValue("$notBefore", (notBeforeUtc ?? now).ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$now", now.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<JobLease?> LeaseNextAsync(
        string workerId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var now = _timeProvider.GetUtcNow();
        var leasedUntil = now + leaseDuration;

        // A raw "BEGIN IMMEDIATE" (rather than ADO.NET's BeginTransaction(), which issues a lazy
        // "BEGIN DEFERRED" that only takes SQLite's write lock on the first write) grabs the
        // single writer lock up front. A second worker's concurrent BEGIN IMMEDIATE then blocks
        // on that lock (honoring `PRAGMA busy_timeout`) until this transaction commits or rolls
        // back, so the SELECT-then-UPDATE below is atomic across processes, not just threads.
        await ExecuteAsync(connection, "BEGIN IMMEDIATE;", cancellationToken).ConfigureAwait(false);
        try
        {
            long? candidateId;
            using (var select = connection.CreateCommand())
            {
                // Runnable = still-pending jobs whose schedule has arrived, OR jobs whose lease
                // expired without the worker completing/failing them (crash recovery).
                select.CommandText = """
                    SELECT id FROM jobs
                    WHERE (state = $pending AND not_before_utc <= $now)
                       OR (state = $leased AND leased_until < $now)
                    ORDER BY not_before_utc ASC
                    LIMIT 1;
                    """;
                select.Parameters.AddWithValue("$pending", JobStates.Pending);
                select.Parameters.AddWithValue("$leased", JobStates.Leased);
                select.Parameters.AddWithValue("$now", now.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));

                var result = await select.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                candidateId = result is null or DBNull ? null : Convert.ToInt64(result, CultureInfo.InvariantCulture);
            }

            if (candidateId is null)
            {
                await ExecuteAsync(connection, "COMMIT;", cancellationToken).ConfigureAwait(false);
                return null;
            }

            using (var update = connection.CreateCommand())
            {
                update.CommandText = """
                    UPDATE jobs
                    SET state = $leasedState,
                        attempts = attempts + 1,
                        leased_until = $leasedUntil,
                        worker_id = $workerId,
                        updated_at_utc = $now
                    WHERE id = $id;
                    """;
                update.Parameters.AddWithValue("$leasedState", JobStates.Leased);
                update.Parameters.AddWithValue("$leasedUntil", leasedUntil.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
                update.Parameters.AddWithValue("$workerId", workerId);
                update.Parameters.AddWithValue("$now", now.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
                update.Parameters.AddWithValue("$id", candidateId.Value);
                await update.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            string type;
            string payload;
            long attempts;
            using (var read = connection.CreateCommand())
            {
                read.CommandText = "SELECT type, payload, attempts FROM jobs WHERE id = $id;";
                read.Parameters.AddWithValue("$id", candidateId.Value);
                using var reader = await read.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
                type = reader.GetString(0);
                payload = reader.GetString(1);
                attempts = reader.GetInt64(2);
            }

            await ExecuteAsync(connection, "COMMIT;", cancellationToken).ConfigureAwait(false);

            return new JobLease(candidateId.Value.ToString(CultureInfo.InvariantCulture), type, payload, (int)attempts, leasedUntil);
        }
        catch
        {
            await ExecuteAsync(connection, "ROLLBACK;", cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    public async Task CompleteAsync(string jobId, CancellationToken cancellationToken = default)
    {
        var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var now = _timeProvider.GetUtcNow();

        using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE jobs
            SET state = $completed,
                leased_until = NULL,
                worker_id = NULL,
                updated_at_utc = $now
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$completed", JobStates.Completed);
        command.Parameters.AddWithValue("$now", now.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$id", long.Parse(jobId, CultureInfo.InvariantCulture));
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task FailAsync(
        string jobId,
        string error,
        TimeSpan? retryAfter = null,
        CancellationToken cancellationToken = default)
    {
        var connection = await OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var now = _timeProvider.GetUtcNow();
        var id = long.Parse(jobId, CultureInfo.InvariantCulture);

        long attempts;
        using (var read = connection.CreateCommand())
        {
            read.CommandText = "SELECT attempts FROM jobs WHERE id = $id;";
            read.Parameters.AddWithValue("$id", id);
            var result = await read.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            attempts = result is null or DBNull ? 0 : Convert.ToInt64(result, CultureInfo.InvariantCulture);
        }

        var exhausted = attempts >= MaxAttempts;
        var nextState = exhausted ? JobStates.Failed : JobStates.Pending;
        var backoff = retryAfter ?? ComputeBackoff((int)attempts);
        var notBefore = now + backoff;

        using var update = connection.CreateCommand();
        update.CommandText = """
            UPDATE jobs
            SET state = $state,
                not_before_utc = $notBefore,
                leased_until = NULL,
                worker_id = NULL,
                last_error = $error,
                updated_at_utc = $now
            WHERE id = $id;
            """;
        update.Parameters.AddWithValue("$state", nextState);
        update.Parameters.AddWithValue("$notBefore", notBefore.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        update.Parameters.AddWithValue("$error", error);
        update.Parameters.AddWithValue("$now", now.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));
        update.Parameters.AddWithValue("$id", id);
        await update.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Exponential backoff with a fixed ceiling: <c>min(base * 2^attempts, max)</c>.</summary>
    internal static TimeSpan ComputeBackoff(int attempts)
    {
        // Cap the exponent, not just the result: base(30s) * 2^11 already exceeds MaxBackoff
        // (1h), so anything beyond that would only risk overflowing TimeSpan's range for large
        // attempt counts (e.g. a job that has failed hundreds of times) with no behavioral
        // difference — the clamp below still applies.
        var exponent = Math.Clamp(attempts - 1, 0, 11);
        var factor = Math.Pow(2, exponent);
        var candidate = BaseBackoff * factor;
        return candidate > MaxBackoff ? MaxBackoff : candidate;
    }

    private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = (SqliteConnection)_dbContext.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
        }

        return connection;
    }

    private static async Task ExecuteAsync(SqliteConnection connection, string sql, CancellationToken cancellationToken)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
