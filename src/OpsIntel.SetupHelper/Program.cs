using OpsIntel.SetupHelper.Cli;
using OpsIntel.SetupHelper.Commands;

namespace OpsIntel.SetupHelper;

/// <summary>
/// Entry point for <c>OpsIntel.SetupHelper.exe</c>: the deferred, <c>Impersonate="no"</c>
/// custom-action helper installer/Cert.wxs invokes as SYSTEM instead of a PowerShell custom
/// action (msi_kurulum_dagitim.md §5). Deliberately prints no secrets (no key material,
/// passwords, or full certificate bodies) — only thumbprints, paths and short status text — so
/// its stdout is safe to capture in an MSI log.
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        var parsed = ArgumentParser.Parse(args);

        try
        {
            return Dispatch(parsed, Console.Out);
        }
        catch (Exception ex)
        {
            // Never let an unexpected exception's message leak potentially sensitive detail
            // (e.g. a file path under a user profile); log the exception type and a short
            // reason only.
            Console.Error.WriteLine($"OpsIntel.SetupHelper failed: {ex.GetType().Name}: {ex.Message}");
            return ExitCodes.UnhandledError;
        }
    }

    private static int Dispatch(ParsedArguments parsed, TextWriter output)
    {
        switch (parsed.Verb?.ToLowerInvariant())
        {
            case "cert":
                return DispatchCert(parsed, output);

            case "port":
                return DispatchPort(parsed, output);

            case "db":
                return DispatchDb(parsed, output);

            default:
                PrintUsage(output);
                return ExitCodes.InvalidArguments;
        }
    }

    private static int DispatchCert(ParsedArguments parsed, TextWriter output)
    {
        switch (parsed.Noun?.ToLowerInvariant())
        {
            case "create":
                return CertCommands.Create(
                    parsed.GetString("thumbprint"),
                    parsed.GetInt("port") ?? 6500,
                    output);

            case "remove":
                return CertCommands.Remove(output);

            case "renew":
                return CertCommands.Renew(output);

            default:
                output.WriteLine("Usage: OpsIntel.SetupHelper cert <create|remove|renew> [--thumbprint=<t>] [--port=<n>]");
                return ExitCodes.InvalidArguments;
        }
    }

    private static int DispatchPort(ParsedArguments parsed, TextWriter output)
    {
        if (!string.Equals(parsed.Noun, "check", StringComparison.OrdinalIgnoreCase))
        {
            output.WriteLine("Usage: OpsIntel.SetupHelper port check --port=<n>");
            return ExitCodes.InvalidArguments;
        }

        var port = parsed.GetInt("port");
        if (port is null)
        {
            output.WriteLine("port check requires --port=<n>.");
            return ExitCodes.InvalidArguments;
        }

        return PortCommands.Check(port.Value, output);
    }

    private static int DispatchDb(ParsedArguments parsed, TextWriter output)
    {
        if (!string.Equals(parsed.Noun, "backup", StringComparison.OrdinalIgnoreCase))
        {
            output.WriteLine("Usage: OpsIntel.SetupHelper db backup --out=<path> [--db=<path>]");
            return ExitCodes.InvalidArguments;
        }

        return DbCommands.Backup(parsed.GetString("db"), parsed.GetString("out"), output);
    }

    private static void PrintUsage(TextWriter output)
    {
        output.WriteLine("""
            OpsIntel.SetupHelper - MSI installer helper (ADR-0004, ADR-0012)

            Usage:
              OpsIntel.SetupHelper cert create [--thumbprint=<corp cert thumbprint>] [--port=<n>]
              OpsIntel.SetupHelper cert remove [--port=<n>]
              OpsIntel.SetupHelper cert renew
              OpsIntel.SetupHelper port check --port=<n>
              OpsIntel.SetupHelper db backup --out=<path> [--db=<path>]

            Options accept --name=value, --name value, /name:value, or /name value.
            """);
    }
}
