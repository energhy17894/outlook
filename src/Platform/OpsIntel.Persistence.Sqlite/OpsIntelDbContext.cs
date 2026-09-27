using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace OpsIntel.Persistence.Sqlite;

/// <summary>
/// EF Core SQLite context for the local OpsIntel database (ADR-0010). Enables WAL journal mode
/// on every connection open so the Host and Intelligence services can read/write concurrently
/// without blocking each other.
/// </summary>
/// <remarks>
/// TODO(Faz 0): this skeleton uses <c>EnsureCreated</c> for developer convenience. Before MVP,
/// replace it with a proper EF Core migrations history (`dotnet ef migrations add Initial`) so
/// schema upgrades can run as part of the MSI major-upgrade path (ADR-0022).
/// </remarks>
public sealed class OpsIntelDbContext : DbContext
{
    public OpsIntelDbContext(DbContextOptions<OpsIntelDbContext> options)
        : base(options)
    {
    }

    public DbSet<ProjectRow> Projects => Set<ProjectRow>();
    public DbSet<WorkItemRow> WorkItems => Set<WorkItemRow>();
    public DbSet<EvidenceRow> Evidence => Set<EvidenceRow>();
    public DbSet<AuditEventRow> AuditEvents => Set<AuditEventRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProjectRow>(e =>
        {
            e.ToTable("projects");
            e.HasKey(p => p.Id);
            e.HasMany(p => p.WorkItems).WithOne(w => w.Project).HasForeignKey(w => w.ProjectId);
        });

        modelBuilder.Entity<WorkItemRow>(e =>
        {
            e.ToTable("work_items");
            e.HasKey(w => w.Id);
            e.HasMany(w => w.Evidence).WithOne(ev => ev.WorkItem).HasForeignKey(ev => ev.WorkItemId);
        });

        modelBuilder.Entity<EvidenceRow>(e =>
        {
            e.ToTable("evidence");
            e.HasKey(ev => ev.Id);
        });

        modelBuilder.Entity<AuditEventRow>(e =>
        {
            e.ToTable("audit_event");
            e.HasKey(a => a.SequenceNumber);
            e.Property(a => a.SequenceNumber).ValueGeneratedOnAdd();
            e.HasIndex(a => a.EventId).IsUnique();
        });
    }

    /// <summary>
    /// Opens the underlying connection (if not already open) and switches it to WAL journal
    /// mode, as required by ADR-0010 for concurrent Host/Intelligence access.
    /// </summary>
    public void EnsureWalModeEnabled()
    {
        var connection = Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            connection.Open();
        }

        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode = 'wal';";
        command.ExecuteNonQuery();
    }
}

/// <summary>Factory helpers for building an <see cref="OpsIntelDbContext"/> outside DI (tests, tools).</summary>
public static class OpsIntelDbContextFactory
{
    public static OpsIntelDbContext Create(string sqliteConnectionString)
    {
        var options = new DbContextOptionsBuilder<OpsIntelDbContext>()
            .UseSqlite(new SqliteConnection(sqliteConnectionString))
            .Options;

        var context = new OpsIntelDbContext(options);
        context.EnsureWalModeEnabled();
        context.Database.EnsureCreated();
        return context;
    }
}
