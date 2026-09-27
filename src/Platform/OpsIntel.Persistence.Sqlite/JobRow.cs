namespace OpsIntel.Persistence.Sqlite;

/// <summary>
/// The durable job/outbox row backing <see cref="SqliteJobQueue"/> (ADR-0012: DB job/outbox +
/// Channels + Quartz.NET; no Temporal/Dapr/Hangfire). A transactional outbox write couples a
/// module's own state change with the enqueue of the row below, in the same SQLite transaction,
/// so *intent* is recorded exactly once even though *execution* is only guaranteed at least once.
/// </summary>
public sealed class JobRow
{
    public long Id { get; set; }

    /// <summary>Logical job type (dispatch key for the worker side).</summary>
    public string Type { get; set; } = default!;

    /// <summary>Opaque JSON payload the worker deserializes.</summary>
    public string Payload { get; set; } = default!;

    /// <summary>
    /// Caller-supplied idempotency key (e.g. Graph ID + changeKey, content hash + prompt
    /// version, action ID). Unique: re-enqueueing the same logical work is a no-op.
    /// </summary>
    public string IdempotencyKey { get; set; } = default!;

    /// <summary>One of <see cref="JobStates"/>.</summary>
    public string State { get; set; } = JobStates.Pending;

    public int Attempts { get; set; }

    /// <summary>The job is not eligible for lease before this time (schedule / backoff).</summary>
    public DateTimeOffset NotBeforeUtc { get; set; }

    /// <summary>While leased, the time after which the lease is considered expired and can be re-leased.</summary>
    public DateTimeOffset? LeasedUntilUtc { get; set; }

    /// <summary>Identifies the worker instance currently holding the lease, for diagnostics.</summary>
    public string? WorkerId { get; set; }

    public string? LastError { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; }
}

/// <summary>The finite set of states a <see cref="JobRow"/> can be in.</summary>
public static class JobStates
{
    /// <summary>Enqueued, waiting for <see cref="JobRow.NotBeforeUtc"/> and a free lease.</summary>
    public const string Pending = "pending";

    /// <summary>Currently leased to a worker.</summary>
    public const string Leased = "leased";

    /// <summary>Terminal: finished successfully.</summary>
    public const string Completed = "completed";

    /// <summary>
    /// Terminal: failed and exhausted its retry budget. Visible to operators as a dead letter
    /// (ADR-0012).
    /// </summary>
    public const string Failed = "failed";
}
