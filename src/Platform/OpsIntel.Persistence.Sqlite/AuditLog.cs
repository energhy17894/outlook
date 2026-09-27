using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace OpsIntel.Persistence.Sqlite;

/// <summary>
/// Appends and verifies the hash-chained <c>audit_event</c> log (ADR-0018). Each event's
/// <see cref="AuditEventRow.Hash"/> is <c>SHA-256(PrevHash ‖ CanonicalPayload)</c>, so altering
/// or deleting any past row invalidates every hash after it — <see cref="VerifyChainAsync"/>
/// detects this deterministically without needing an external trust anchor.
/// </summary>
public sealed class AuditLog
{
    private readonly OpsIntelDbContext _dbContext;

    public AuditLog(OpsIntelDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>Appends a new audit event, chaining it to the current tail of the log.</summary>
    public async Task<AuditEventRow> AppendAsync(
        string eventId,
        string actorJson,
        string eventType,
        string subject,
        string payloadJson,
        CancellationToken cancellationToken = default)
    {
        var previous = await _dbContext.AuditEvents
            .OrderByDescending(e => e.SequenceNumber)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        var timestamp = DateTimeOffset.UtcNow;
        var canonicalPayload = CanonicalPayload(eventId, timestamp, actorJson, eventType, subject, payloadJson);
        var hash = ComputeHash(previous?.Hash, canonicalPayload);

        var row = new AuditEventRow
        {
            EventId = eventId,
            TimestampUtc = timestamp,
            ActorJson = actorJson,
            EventType = eventType,
            Subject = subject,
            PayloadJson = payloadJson,
            PrevHash = previous?.Hash,
            Hash = hash,
        };

        _dbContext.AuditEvents.Add(row);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return row;
    }

    /// <summary>
    /// Walks the whole chain in sequence order and recomputes every hash, returning
    /// <c>true</c> only if every row's stored hash matches its recomputed hash and correctly
    /// chains to the previous row.
    /// </summary>
    public async Task<bool> VerifyChainAsync(CancellationToken cancellationToken = default)
    {
        string? expectedPrevHash = null;

        await foreach (var row in _dbContext.AuditEvents
            .OrderBy(e => e.SequenceNumber)
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken))
        {
            if (row.PrevHash != expectedPrevHash)
            {
                return false;
            }

            var canonicalPayload = CanonicalPayload(
                row.EventId, row.TimestampUtc, row.ActorJson, row.EventType, row.Subject, row.PayloadJson);
            var recomputedHash = ComputeHash(row.PrevHash, canonicalPayload);

            if (recomputedHash != row.Hash)
            {
                return false;
            }

            expectedPrevHash = row.Hash;
        }

        return true;
    }

    private static string CanonicalPayload(
        string eventId,
        DateTimeOffset timestampUtc,
        string actorJson,
        string eventType,
        string subject,
        string payloadJson)
    {
        // A fixed field order/JSON shape so the same logical event always canonicalizes
        // identically, regardless of caller-supplied JSON formatting.
        return JsonSerializer.Serialize(new
        {
            eventId,
            timestampUtc,
            actorJson,
            eventType,
            subject,
            payloadJson,
        });
    }

    private static string ComputeHash(string? prevHash, string canonicalPayload)
    {
        var bytes = Encoding.UTF8.GetBytes((prevHash ?? string.Empty) + canonicalPayload);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}
