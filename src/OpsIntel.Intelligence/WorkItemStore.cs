using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OpsIntel.AI.Extraction;
using OpsIntel.Contracts;
using OpsIntel.AI.Extraction.WorkItems;
using OpsIntel.Persistence.Sqlite;

namespace OpsIntel.Intelligence;

/// <summary>
/// Persists verified <c>work_items</c> extraction output as <see cref="WorkItemRow"/> +
/// <see cref="EvidenceRow"/> (ADR-0015: only items with verified evidence ever reach here).
/// </summary>
/// <remarks>
/// Saving and <see cref="Platform.Abstractions.IJobQueue.CompleteAsync"/> are not one
/// transaction, so a crash in between re-runs the job. Items therefore get deterministic IDs
/// and existing IDs are skipped, never overwritten — a re-run can neither duplicate an item
/// nor clobber a review decision a person already made on it.
/// </remarks>
public sealed class WorkItemStore
{
    private readonly OpsIntelDbContext _db;

    public WorkItemStore(OpsIntelDbContext db) => _db = db;

    /// <summary>Inserts the outcome's items not already stored; returns how many were new.</summary>
    public async Task<int> SaveAsync(ExtractionThread thread, ExtractionOutcome<RawWorkItem> outcome, CancellationToken cancellationToken)
    {
        var rows = outcome.Items
            .Select(item => ToRow(WorkItemId(thread.ThreadId, item.Source), item))
            .DistinctBy(row => row.Id)
            .ToList();

        var ids = rows.Select(r => r.Id).ToList();
        var existing = await _db.WorkItems
            .Where(w => ids.Contains(w.Id))
            .Select(w => w.Id)
            .ToHashSetAsync(cancellationToken);

        var added = rows.Where(r => !existing.Contains(r.Id)).ToList();
        _db.WorkItems.AddRange(added);
        await _db.SaveChangesAsync(cancellationToken);
        _db.ChangeTracker.Clear(); // process-lifetime context: don't accumulate tracked rows
        return added.Count;
    }

    /// <summary>
    /// The stable identity of one extracted item within a thread: re-extracting the same thread
    /// must yield the same ID for "the same" item, and a different ID for a genuinely new one.
    /// </summary>
    private static string WorkItemId(string threadId, RawWorkItem item)
    {
        // Anchor on the first evidence quote (copied verbatim from the source, so it survives
        // the model rewording the title between runs) rather than on Title. Whitespace is
        // collapsed so re-wrapped quotes still match; \u001F can't occur in mail text, so field
        // boundaries can't be forged by the content. Hashing keeps mail text out of the ID.
        var evidence = item.Evidence.Count > 0 ? item.Evidence[0] : new RawEvidence("", item.Title);
        var quote = string.Join(' ', evidence.Quote.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        var key = string.Join('\u001F', threadId, item.Kind, evidence.MessageId, quote);
        return "wi_" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)))[..32].ToLowerInvariant();
    }

    private static WorkItemRow ToRow(string id, VerifiedItem<RawWorkItem> item) => new()
    {
        Id = id,
        ProjectId = null,
        Kind = item.Source.Kind,
        Title = item.Source.Title,
        Status = item.Source.Status,
        ReviewState = "Suggested",
        DueAtUtc = DateOnly.TryParseExact(item.Source.DueDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var due)
            ? new DateTimeOffset(due.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)
            : null,
        Evidence = item.VerifiedEvidence
            .Select((ev, i) => new EvidenceRow
            {
                Id = $"{id}:{i}",
                WorkItemId = id,
                RawItemId = ev.MessageId,
                ExactQuote = ev.Quote,
                CharStart = ev.CharStart!.Value, // VerifiedEvidence entries always carry offsets
                CharEnd = ev.CharEnd!.Value,
                Verified = true,
            })
            .ToList(),
    };
}
