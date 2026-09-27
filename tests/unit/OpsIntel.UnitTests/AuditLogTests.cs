using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OpsIntel.Persistence.Sqlite;
using Xunit;

namespace OpsIntel.UnitTests;

public sealed class AuditLogTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly OpsIntelDbContext _dbContext;
    private readonly AuditLog _auditLog;

    public AuditLogTests()
    {
        // A single shared in-memory connection kept open for the test's lifetime, per the
        // standard EF Core SQLite in-memory testing pattern.
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<OpsIntelDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new OpsIntelDbContext(options);
        _dbContext.Database.EnsureCreated();
        _auditLog = new AuditLog(_dbContext);
    }

    [Fact]
    public async Task VerifyChainAsync_ReturnsTrue_ForUntamperedChain()
    {
        await _auditLog.AppendAsync("evt-1", "{}", "ProjectCreated", "project/1", "{}");
        await _auditLog.AppendAsync("evt-2", "{}", "WorkItemAccepted", "workitem/1", "{}");
        await _auditLog.AppendAsync("evt-3", "{}", "ApprovalGranted", "proposal/1", "{}");

        Assert.True(await _auditLog.VerifyChainAsync());
    }

    [Fact]
    public async Task VerifyChainAsync_DetectsPayloadTampering()
    {
        await _auditLog.AppendAsync("evt-1", "{}", "ProjectCreated", "project/1", "{}");
        await _auditLog.AppendAsync("evt-2", "{}", "WorkItemAccepted", "workitem/1", "{}");

        var tampered = await _dbContext.AuditEvents.OrderBy(e => e.SequenceNumber).FirstAsync();
        tampered.PayloadJson = "{\"tampered\":true}";
        await _dbContext.SaveChangesAsync();

        Assert.False(await _auditLog.VerifyChainAsync());
    }

    [Fact]
    public async Task VerifyChainAsync_DetectsHashOverwrite()
    {
        await _auditLog.AppendAsync("evt-1", "{}", "ProjectCreated", "project/1", "{}");
        await _auditLog.AppendAsync("evt-2", "{}", "WorkItemAccepted", "workitem/1", "{}");

        var tampered = await _dbContext.AuditEvents.OrderBy(e => e.SequenceNumber).FirstAsync();
        tampered.Hash = new string('0', tampered.Hash.Length);
        await _dbContext.SaveChangesAsync();

        Assert.False(await _auditLog.VerifyChainAsync());
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
