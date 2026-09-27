namespace OpsIntel.Contracts;

/// <summary>
/// Who (or what) performed an action recorded in an <see cref="AuditEvent"/>.
/// </summary>
public sealed record AuditActor(
    string? UserObjectId,
    string? ServiceName,
    string? ModelId);

/// <summary>
/// An append-only, hash-chained audit entry (ADR-0018): <c>Hash = SHA-256(PrevHash ‖
/// CanonicalPayloadJson)</c>. Tampering with any past event breaks every subsequent hash,
/// which <c>OpsIntel.Persistence.Sqlite</c>'s audit log verifier detects.
/// </summary>
public sealed record AuditEvent(
    string EventId,
    DateTimeOffset TimestampUtc,
    AuditActor Actor,
    string EventType,
    string Subject,
    string PayloadJson,
    string? PrevHash,
    string Hash);
