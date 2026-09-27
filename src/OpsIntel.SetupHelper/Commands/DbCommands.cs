using Microsoft.Data.Sqlite;
using OpsIntel.SetupHelper.Cli;
using OpsIntel.SetupHelper.Config;

namespace OpsIntel.SetupHelper.Commands;

/// <summary>
/// <c>db backup --out &lt;path&gt; [--db &lt;path&gt;]</c>: a live, consistent snapshot of the
/// SQLite database using SQLite's online backup API
/// (<see cref="SqliteConnection.BackupDatabase(SqliteConnection)"/>), which is safe to run
/// while the Host/Intelligence services hold the database open in WAL mode (ADR-0010) — unlike
/// a plain file copy, it does not need the writers to be stopped.
/// </summary>
public static class DbCommands
{
    /// <summary>Default database file name under the ADR-0010/Folders.wxs <c>DataDir</c>.</summary>
    public const string DefaultDatabaseFileName = "app.db";

    public static int Backup(string? sourcePathArg, string? outPathArg, TextWriter output)
    {
        if (string.IsNullOrWhiteSpace(outPathArg))
        {
            output.WriteLine("db backup requires --out <path>.");
            return ExitCodes.InvalidArguments;
        }

        string sourcePath;
        if (!string.IsNullOrWhiteSpace(sourcePathArg))
        {
            sourcePath = sourcePathArg;
        }
        else if (OperatingSystem.IsWindows())
        {
            var dataDir = OpsIntelRegistryConfig.GetDataDir();
            if (string.IsNullOrWhiteSpace(dataDir))
            {
                output.WriteLine("No --db path was given and HKLM\\SOFTWARE\\OpsIntel\\DataDir is not set.");
                return ExitCodes.InvalidArguments;
            }

            sourcePath = Path.Combine(dataDir, DefaultDatabaseFileName);
        }
        else
        {
            output.WriteLine("db backup requires --db <path> when not running on Windows (no registry to read DataDir from).");
            return ExitCodes.InvalidArguments;
        }

        if (!File.Exists(sourcePath))
        {
            output.WriteLine($"Source database not found: {sourcePath}");
            return ExitCodes.InvalidArguments;
        }

        var outDirectory = Path.GetDirectoryName(Path.GetFullPath(outPathArg));
        if (!string.IsNullOrEmpty(outDirectory))
        {
            Directory.CreateDirectory(outDirectory);
        }

        // Read-only on the source: the online backup API copies pages under SQLite's own
        // locking, so this never blocks (or is blocked by) a writer for more than a single page
        // at a time, unlike holding an exclusive OS-level file lock.
        using var source = new SqliteConnection($"Data Source={sourcePath};Mode=ReadOnly;");
        using var destination = new SqliteConnection($"Data Source={outPathArg}");
        source.Open();
        destination.Open();

        source.BackupDatabase(destination);

        output.WriteLine($"Backed up '{sourcePath}' to '{outPathArg}'.");
        return ExitCodes.Success;
    }
}
