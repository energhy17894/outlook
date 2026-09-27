namespace OpsIntel.Contracts;

/// <summary>
/// A user-curated project registry entry (report §3: "kayıt defterine karşı sınıflandırma" —
/// classification against a registry, never free clustering). <see cref="CurrentPhase"/> is
/// driven by a configurable finite-state machine; illegal jumps are rejected (ADR-0016).
/// </summary>
public sealed record Project(
    string Id,
    string Name,
    IReadOnlyList<string> Aliases,
    string? CustomerOrganizationId,
    string CurrentPhase,
    ProjectHealth Health);

/// <summary>
/// A rolling health assessment for a <see cref="Project"/>, backed by evidence rather than
/// a single opaque score.
/// </summary>
public sealed record ProjectHealth(
    double Score,
    string Band,
    IReadOnlyList<string> Drivers);

/// <summary>
/// A recorded (or inferred) move of a <see cref="Project"/> from one lifecycle phase to
/// another. <see cref="Inferred"/> transitions require human confirmation before the
/// project's <see cref="Project.CurrentPhase"/> is updated.
/// </summary>
public sealed record PhaseTransition(
    string Id,
    string ProjectId,
    string FromPhase,
    string ToPhase,
    DateTimeOffset OccurredAtUtc,
    double Probability,
    IReadOnlyList<string> EvidenceIds,
    bool Inferred);
