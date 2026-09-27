using System.Runtime.CompilerServices;

// Lets OpsIntel.SetupHelper.Tests exercise SqliteJobQueue's internal backoff calculation
// directly (ComputeBackoff), rather than only through its externally-observable effect on
// FailAsync's not_before_utc scheduling.
[assembly: InternalsVisibleTo("OpsIntel.SetupHelper.Tests")]
