namespace OpsIntel.SetupHelper.Cli;

/// <summary>
/// A small, dependency-free option parser. The installer's own CLI contract is a moving
/// target between Cert.wxs's <c>--port=[PORT]</c> / <c>--thumbprint=[CERT_THUMBPRINT]</c>
/// (WixQuietExec, double-quoted single command line) and Package.wxs's TODO comment, which
/// instead writes <c>/port:[PORT]</c> (classic Windows slash-colon style). This parser accepts
/// both forms, plus a plain space-separated <c>--name value</c>, so a change in either .wxs
/// file's exact quoting does not silently break the contract:
///   --name=value | --name value | /name:value | /name value
/// Option names are matched case-insensitively; a bare flag (no value) is recorded as "true".
/// </summary>
public static class ArgumentParser
{
    public static ParsedArguments Parse(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
        {
            return new ParsedArguments(null, null, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        }

        string? verb = null;
        string? noun = null;
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var index = 0;

        // The first one or two non-option tokens are the command, e.g. "cert create",
        // "port check", "db backup". A single-word command ("cert" with no noun) is allowed
        // too, and is rejected later by the command dispatcher if it needs a noun.
        if (index < args.Count && !IsOption(args[index]))
        {
            verb = args[index];
            index++;
        }

        if (index < args.Count && !IsOption(args[index]))
        {
            noun = args[index];
            index++;
        }

        while (index < args.Count)
        {
            var token = args[index];
            index++;

            if (!IsOption(token))
            {
                // Unrecognized positional token; ignore rather than fail, so an installer that
                // passes an extra, forward-compatible argument doesn't break the whole action.
                continue;
            }

            var (name, inlineValue) = SplitInline(token);

            if (inlineValue is not null)
            {
                options[name] = inlineValue;
                continue;
            }

            // "--name value" / "/name value": only consume the next token as a value if it does
            // not itself look like another option, otherwise this is a bare boolean flag.
            if (index < args.Count && !IsOption(args[index]))
            {
                options[name] = args[index];
                index++;
            }
            else
            {
                options[name] = "true";
            }
        }

        return new ParsedArguments(verb, noun, options);
    }

    private static bool IsOption(string token)
        => token.StartsWith("--", StringComparison.Ordinal) || token.StartsWith('/');

    private static (string Name, string? InlineValue) SplitInline(string token)
    {
        string body = token.StartsWith("--", StringComparison.Ordinal) ? token[2..] : token[1..];

        // "--name=value"
        var eq = body.IndexOf('=');
        if (eq >= 0)
        {
            return (body[..eq], body[(eq + 1)..]);
        }

        // "/name:value" (classic Windows CLI style; "--name:value" is accepted too for symmetry)
        var colon = body.IndexOf(':');
        if (colon >= 0)
        {
            return (body[..colon], body[(colon + 1)..]);
        }

        return (body, null);
    }
}

/// <summary>The parsed command line: an optional verb/noun pair plus a case-insensitive option bag.</summary>
public sealed class ParsedArguments(string? verb, string? noun, IReadOnlyDictionary<string, string> options)
{
    public string? Verb { get; } = verb;
    public string? Noun { get; } = noun;
    private readonly IReadOnlyDictionary<string, string> _options = options;

    public string? GetString(string name) => _options.TryGetValue(name, out var value) ? value : null;

    public int? GetInt(string name)
        => _options.TryGetValue(name, out var value) && int.TryParse(value, out var parsed) ? parsed : null;

    public bool Has(string name) => _options.ContainsKey(name);
}
