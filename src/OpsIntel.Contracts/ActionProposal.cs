namespace OpsIntel.Contracts;

/// <summary>
/// Lifecycle of an <see cref="ActionProposal"/>. A human decision (Approved/Rejected/Edited)
/// always sits between a proposal and its execution — see ADR-0017; the approval workflow is
/// a bespoke state machine, not a re-purposed agent-framework checkpoint.
/// </summary>
public enum ApprovalState
{
    Proposed,
    PendingApproval,
    Approved,
    Rejected,
    Edited,
    Executing,
    Done,
    Failed,
}

/// <summary>
/// The kind of action an <see cref="ActionProposal"/> asks a human to approve. Mail sending is
/// intentionally absent from every phase (ADR-0008 / report §4): a "reply_draft" is drafted,
/// never sent, by OpsIntel.
/// </summary>
public enum ActionProposalKind
{
    ReplyDraft,
    Task,
    CalendarHold,
    Nudge,
    StatusReport,
}

/// <summary>
/// A proposed action derived from one or more <see cref="WorkItem"/>s, awaiting (or having
/// received) human approval before an <see cref="Execution"/> is attempted. The executor
/// re-checks policy at execution time, independent of the state recorded here.
/// </summary>
public sealed record ActionProposal(
    string Id,
    ActionProposalKind Kind,
    string PayloadJson,
    string Rationale,
    IReadOnlyList<string> EvidenceIds,
    IReadOnlyList<string> RiskFlags,
    ApprovalState State);

/// <summary>
/// A single approve/reject/edit decision recorded against an <see cref="ActionProposal"/>.
/// </summary>
public sealed record Approval(
    string Id,
    string ActionProposalId,
    string DeciderPersonId,
    ApprovalState Decision,
    DateTimeOffset DecidedAtUtc,
    string? Note);

/// <summary>
/// The outcome of actually carrying out an approved <see cref="ActionProposal"/>. Immutable
/// Graph identifiers and ETags are retained so the effect can be traced back precisely.
/// </summary>
public sealed record Execution(
    string Id,
    string ActionProposalId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    bool Succeeded,
    string? GraphRequestId,
    string? ImmutableId,
    string? ETag,
    string? Error);
