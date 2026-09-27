namespace OpsIntel.Contracts;

/// <summary>
/// The kind of a <see cref="WorkItem"/>, matching the extraction taxonomy in
/// the research report §3 (also aligned with Viva Briefing's Commitment/Request/Follow-up).
/// </summary>
public enum WorkItemKind
{
    Task,
    Commitment,
    Request,
    FollowUp,
    Decision,
    Risk,
    Assumption,
    Issue,
    Dependency,
    OpenQuestion,
    Obligation,
}

/// <summary>
/// Confidence assigned by the extraction pipeline to a proposed <see cref="WorkItem"/>.
/// </summary>
public enum ConfidenceLevel
{
    Low,
    Medium,
    High,
}

/// <summary>
/// The human review lifecycle of an extracted <see cref="WorkItem"/>. Nothing here is
/// ever auto-executed; a person moves an item from <see cref="Suggested"/> onward.
/// </summary>
public enum ReviewState
{
    Suggested,
    Accepted,
    Edited,
    Rejected,
}

/// <summary>
/// A single extracted unit of work (task, commitment, decision, risk, ...). Free text is
/// never stored without an <see cref="Evidence"/> anchor — see ADR-0015.
/// </summary>
public sealed record WorkItem(
    string Id,
    WorkItemKind Kind,
    string Title,
    string ProjectId,
    string? OwnerPersonId,
    string? CounterpartyPersonId,
    DateTimeOffset? DueAtUtc,
    string? DueText,
    string Status,
    ConfidenceLevel Confidence,
    ReviewState ReviewState,
    string? SupersedesId,
    string ExtractorVersion,
    IReadOnlyList<string> EvidenceIds);
