using Microsoft.EntityFrameworkCore;
using OpsIntel.AI.Extraction;
using OpsIntel.AI.Extraction.Verification;
using OpsIntel.AI.Extraction.WorkItems;
using OpsIntel.Intelligence;
using OpsIntel.Persistence.Sqlite;
using Xunit;

namespace OpsIntel.UnitTests;

public sealed class WorkItemStoreTests : IDisposable
{
    private static readonly ExtractionThread Thread = new("thread-1", "Teklif", null, new DateOnly(2026, 10, 1), []);

    private readonly OpsIntelDbContext _db = OpsIntelDbContextFactory.Create("Data Source=:memory:");

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task SaveAsync_SameOutcomeTwice_StoresEachItemOnce()
    {
        var store = new WorkItemStore(_db);
        var outcome = Outcome(Item("commitment", "Teklifi cuma gönder", "2026-10-02"), Item("request", "Fiyat listesini paylaş", null));

        Assert.Equal(2, await store.SaveAsync(Thread, outcome, CancellationToken.None));
        Assert.Equal(0, await store.SaveAsync(Thread, outcome, CancellationToken.None)); // job re-run after a crash

        var rows = await _db.WorkItems.Include(w => w.Evidence).AsNoTracking().ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.All(rows, r => Assert.Null(r.ProjectId));
        Assert.Equal(new DateTimeOffset(2026, 10, 2, 0, 0, 0, TimeSpan.Zero), rows.Single(r => r.Kind == "commitment").DueAtUtc);
        Assert.All(rows, r => Assert.Equal("msg-1", Assert.Single(r.Evidence).RawItemId));
    }

    [Fact]
    public async Task SaveAsync_SameItemInAnotherThread_IsADifferentItem()
    {
        var store = new WorkItemStore(_db);
        var outcome = Outcome(Item("task", "Raporu hazırla", null));

        await store.SaveAsync(Thread, outcome, CancellationToken.None);
        Assert.Equal(1, await store.SaveAsync(Thread with { ThreadId = "thread-2" }, outcome, CancellationToken.None));
    }

    private static ExtractionOutcome<RawWorkItem> Outcome(params VerifiedItem<RawWorkItem>[] items) => new(items, 0);

    private static VerifiedItem<RawWorkItem> Item(string kind, string title, string? dueDate) => new(
        new RawWorkItem(kind, title, null, null, dueDate, null, "open", "high", false, null, [new RawEvidence("msg-1", title)]),
        [new VerifiedEvidence("msg-1", title, true, 0, title.Length, QuoteFailureReason.None)],
        NeedsReview: false);
}
