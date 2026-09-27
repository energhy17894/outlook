namespace OpsIntel.Persistence.Sqlite;

// Minimal persistence-layer row shapes for the Faz 0 skeleton (report §3 / §5). These are
// intentionally thinner than OpsIntel.Contracts' DTOs; modules map between the two so the
// storage schema can evolve independently of the wire/contract shape.

public sealed class ProjectRow
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string CurrentPhase { get; set; } = default!;
    public string? CustomerOrganizationId { get; set; }

    public List<WorkItemRow> WorkItems { get; set; } = [];
}

public sealed class WorkItemRow
{
    public string Id { get; set; } = default!;
    public string ProjectId { get; set; } = default!;
    public string Kind { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string ReviewState { get; set; } = default!;
    public DateTimeOffset? DueAtUtc { get; set; }

    public ProjectRow Project { get; set; } = default!;
    public List<EvidenceRow> Evidence { get; set; } = [];
}

public sealed class EvidenceRow
{
    public string Id { get; set; } = default!;
    public string WorkItemId { get; set; } = default!;
    public string RawItemId { get; set; } = default!;
    public string ExactQuote { get; set; } = default!;
    public int CharStart { get; set; }
    public int CharEnd { get; set; }
    public bool Verified { get; set; }

    public WorkItemRow WorkItem { get; set; } = default!;
}

/// <summary>
/// An append-only, hash-chained audit row (ADR-0018). <see cref="Hash"/> =
/// SHA-256(<see cref="PrevHash"/> ‖ canonical JSON of the event's own fields); see
/// <see cref="AuditLog"/> for how the chain is built and verified.
/// </summary>
public sealed class AuditEventRow
{
    public long SequenceNumber { get; set; }
    public string EventId { get; set; } = default!;
    public DateTimeOffset TimestampUtc { get; set; }
    public string ActorJson { get; set; } = default!;
    public string EventType { get; set; } = default!;
    public string Subject { get; set; } = default!;
    public string PayloadJson { get; set; } = default!;
    public string? PrevHash { get; set; }
    public string Hash { get; set; } = default!;
}
