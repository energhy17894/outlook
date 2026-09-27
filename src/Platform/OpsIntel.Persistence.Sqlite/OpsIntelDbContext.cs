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
    public DbSet<JobRow> Jobs => Set<JobRow>();

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

        // Durable job/outbox queue (ADR-0012). idempotency_key is UNIQUE so re-enqueueing the
        // same logical work is a no-op (SqliteJobQueue.EnqueueAsync relies on this constraint
        // via `INSERT ... ON CONFLICT(idempotency_key) DO NOTHING`).
        modelBuilder.Entity<JobRow>(e =>
        {
            e.ToTable("jobs");
            e.HasKey(j => j.Id);
            e.Property(j => j.Id).HasColumnName("id").ValueGeneratedOnAdd();
            e.Property(j => j.Type).HasColumnName("type").IsRequired();
            e.Property(j => j.Payload).HasColumnName("payload").IsRequired();
            e.Property(j => j.IdempotencyKey).HasColumnName("idempotency_key").IsRequired();
            e.Property(j => j.State).HasColumnName("state").IsRequired();
            e.Property(j => j.Attempts).HasColumnName("attempts");
            e.Property(j => j.NotBeforeUtc).HasColumnName("not_before_utc");
            e.Property(j => j.LeasedUntilUtc).HasColumnName("leased_until");
            e.Property(j => j.WorkerId).HasColumnName("worker_id");
            e.Property(j => j.LastError).HasColumnName("last_error");
            e.Property(j => j.CreatedAtUtc).HasColumnName("created_at_utc");
            e.Property(j => j.UpdatedAtUtc).HasColumnName("updated_at_utc");
            e.HasIndex(j => j.IdempotencyKey).IsUnique();
            e.HasIndex(j => new { j.State, j.NotBeforeUtc });
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
        command.CommandText = "PRAGMA journal_mode = 'wal'; PRAGMA busy_timeout = 5000;";
        command.ExecuteNonQuery();
    }
}

/// <summary>Factory helpers for building an <see cref="OpsIntelDbContext"/> outside DI (tests, tools).</summary>
public static class OpsIntelDbContextFactory
{
    public static OpsIntelDbContext Create(string sqliteConnectionString)
    {
        var options = new DbContextOptionsBuilder<OpsIntelDbContext>()
            // Pass the connection string (not a SqliteConnection instance) so the context owns the
            // connection and closes it on Dispose; otherwise the file handle outlives the context
            // and Windows refuses to delete/move the database file.
            .UseSqlite(sqliteConnectionString)
            .Options;

        var context = new OpsIntelDbContext(options);
        context.EnsureWalModeEnabled();
        context.Database.EnsureCreated();
        return context;
    }
}
