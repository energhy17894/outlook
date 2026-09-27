using System.Text;
using System.Text.RegularExpressions;

namespace OpsIntel.AI.Extraction.Prompts;

/// <summary>A loaded schema + system/user prompt template for one extractor call (e.g. <c>work_items</c>).</summary>
public sealed record PromptSet(string SchemaJson, string SystemPrompt, string UserPromptTemplate);

/// <summary>
/// Loads schema JSON and prompt templates for one extraction call from disk at runtime, per
/// <c>prompts/extraction/v1/README.md</c>'s layout (<c>schemas/&lt;name&gt;.schema.json</c>,
/// <c>templates/&lt;name&gt;.md</c>). The root path is configurable
/// (<see cref="ExtractionOptions.PromptsRoot"/>) so a deployed build can point at wherever the
/// prompt set is installed, independent of the source tree layout.
/// </summary>
/// <remarks>
/// <c>prompts/</c> is a read-only input to this pipeline (owned by the prompt-authoring
/// agent) — this loader only ever reads from it, never writes.
/// </remarks>
public sealed class PromptSetLoader
{
    private readonly string _root;

    public PromptSetLoader(string root)
    {
        _root = root;
    }

    /// <summary>Loads the schema and template markdown for <paramref name="extractorName"/> (e.g. <c>"work_items"</c>) and parses out its system/user prompt sections.</summary>
    public PromptSet Load(string extractorName)
    {
        var schemaPath = Path.Combine(_root, "schemas", $"{extractorName}.schema.json");
        var templatePath = Path.Combine(_root, "templates", $"{extractorName}.md");

        if (!File.Exists(schemaPath))
        {
            throw new FileNotFoundException($"Extraction schema not found: {schemaPath}", schemaPath);
        }

        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Extraction prompt template not found: {templatePath}", templatePath);
        }

        var schemaJson = File.ReadAllText(schemaPath);
        var templateMarkdown = File.ReadAllText(templatePath);
        var (systemPrompt, userTemplate) = ParseTemplateMarkdown(templateMarkdown);

        return new PromptSet(schemaJson, systemPrompt, userTemplate);
    }

    private static readonly Regex FenceBlock = new(@"```(?:[^\n]*)\n(?<body>.*?)```", RegexOptions.Compiled | RegexOptions.Singleline);

    /// <summary>
    /// Extracts the first fenced code block following "## System prompt" and "## User prompt
    /// template" headings, matching the shape every template in <c>templates/</c> uses.
    /// </summary>
    internal static (string SystemPrompt, string UserTemplate) ParseTemplateMarkdown(string markdown)
    {
        var systemPrompt = ExtractFencedBlockAfterHeading(markdown, "## System prompt");
        var userTemplate = ExtractFencedBlockAfterHeading(markdown, "## User prompt template");
        return (systemPrompt, userTemplate);
    }

    private static string ExtractFencedBlockAfterHeading(string markdown, string heading)
    {
        var headingIndex = markdown.IndexOf(heading, StringComparison.Ordinal);
        if (headingIndex < 0)
        {
            throw new InvalidOperationException($"Prompt template is missing the '{heading}' heading.");
        }

        var match = FenceBlock.Match(markdown, headingIndex);
        if (!match.Success)
        {
            throw new InvalidOperationException($"No fenced code block found after '{heading}'.");
        }

        return match.Groups["body"].Value.TrimEnd('\n');
    }
}

/// <summary>
/// Minimal, purpose-built renderer for the <c>{{...}}</c> / <c>{{#each messages}}...{{/each}}</c>
/// placeholder syntax used by every template in <c>prompts/extraction/v1/templates/</c>. Not a
/// general templating engine — the README explicitly leaves the templating engine choice as
/// an <c>OpsIntel.Intelligence</c> implementation detail; this is that decision (a plain
/// string builder, per the README's own suggestion).
/// </summary>
public static class PromptRenderer
{
    private static readonly Regex Placeholder = new(@"\{\{([^}]+)\}\}", RegexOptions.Compiled);
    private const string EachOpen = "{{#each messages}}";
    private const string EachClose = "{{/each}}";

    /// <summary>Renders the user prompt template, substituting scalar variables and repeating the <c>{{#each messages}}</c> block once per message.</summary>
    public static string RenderUserPrompt(
        string template,
        IReadOnlyDictionary<string, string> scalars,
        IReadOnlyList<IReadOnlyDictionary<string, string>> messages)
    {
        var eachStart = template.IndexOf(EachOpen, StringComparison.Ordinal);
        var eachEndTagStart = template.IndexOf(EachClose, StringComparison.Ordinal);
        if (eachStart < 0 || eachEndTagStart < 0)
        {
            return Substitute(template, scalars);
        }

        var before = template[..eachStart];
        var itemTemplate = template[(eachStart + EachOpen.Length)..eachEndTagStart];
        var after = template[(eachEndTagStart + EachClose.Length)..];

        var sb = new StringBuilder();
        sb.Append(Substitute(before, scalars));
        foreach (var messageVars in messages)
        {
            sb.Append(Substitute(itemTemplate, messageVars));
        }

        sb.Append(Substitute(after, scalars));
        return sb.ToString();
    }

    private static string Substitute(string text, IReadOnlyDictionary<string, string> vars)
    {
        return Placeholder.Replace(text, m =>
        {
            var key = m.Groups[1].Value.Trim();

            // "{{project_name_or_"bilinmiyor"}}"-style default-value syntax.
            var orDefault = Regex.Match(key, "^(?<base>.+)_or_\"(?<fallback>.*)\"$");
            if (orDefault.Success)
            {
                var baseKey = orDefault.Groups["base"].Value;
                var fallback = orDefault.Groups["fallback"].Value;
                return vars.TryGetValue(baseKey, out var v) && !string.IsNullOrEmpty(v) ? v : fallback;
            }

            return vars.TryGetValue(key, out var value) ? value : string.Empty;
        });
    }
}
