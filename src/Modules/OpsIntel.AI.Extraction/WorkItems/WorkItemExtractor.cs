using OpsIntel.Contracts;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpsIntel.AI.Extraction.Prompts;

namespace OpsIntel.AI.Extraction.WorkItems;

/// <summary>
/// Runs the <c>work_items</c> extraction call: loads the schema/template from
/// <see cref="ExtractionOptions.PromptsRoot"/>, spotlights every message body, asks the
/// injected <see cref="IChatClient"/> for schema-constrained JSON (no tools — ADR-0013/0019
/// quarantine), then runs every item's evidence through
/// <see cref="EvidenceVerificationEngine"/> before returning.
/// </summary>
public sealed class WorkItemExtractor : IExtractor
{
    private const string ExtractorName = "work_items";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly IChatClient _chatClient;
    private readonly PromptSetLoader _promptSetLoader;
    private readonly ILogger<WorkItemExtractor>? _logger;

    public WorkItemExtractor(
        IChatClient chatClient,
        IOptions<ExtractionOptions> options,
        ILogger<WorkItemExtractor>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(chatClient);
        ArgumentNullException.ThrowIfNull(options);

        _chatClient = chatClient;
        _promptSetLoader = new PromptSetLoader(options.Value.PromptsRoot);
        _logger = logger;
    }

    /// <summary>Constructor for tests/callers that already have a <see cref="PromptSetLoader"/> rooted at a custom path.</summary>
    internal WorkItemExtractor(IChatClient chatClient, PromptSetLoader promptSetLoader, ILogger<WorkItemExtractor>? logger = null)
    {
        _chatClient = chatClient;
        _promptSetLoader = promptSetLoader;
        _logger = logger;
    }

    public async Task<ExtractionOutcome<RawWorkItem>> ExtractWorkItemsAsync(
        ExtractionThread thread, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(thread);

        var promptSet = _promptSetLoader.Load(ExtractorName);
        var userPrompt = BuildUserPrompt(promptSet.UserPromptTemplate, thread);

        using var schemaDocument = JsonDocument.Parse(promptSet.SchemaJson);

        var chatMessages = new List<ChatMessage>
        {
            new(ChatRole.System, promptSet.SystemPrompt),
            new(ChatRole.User, userPrompt),
        };

        var chatOptions = new ChatOptions
        {
            // Quarantined call (ADR-0013/ADR-0019): the extraction model is never given
            // tools. Leaving Tools unset/null is the "no tools" state for IChatClient.
            Tools = null,
            Temperature = 0,
            ResponseFormat = ChatResponseFormat.ForJsonSchema(
                schemaDocument.RootElement.Clone(),
                schemaName: ExtractorName + "_v1",
                schemaDescription: null),
        };

        var response = await _chatClient.GetResponseAsync(chatMessages, chatOptions, cancellationToken)
            .ConfigureAwait(false);

        var rawItems = ParseWorkItems(response.Text);

        var messagesById = thread.Messages.ToDictionary(m => m.Id, m => m.CleanedBody, StringComparer.Ordinal);

        return EvidenceVerificationEngine.Verify(
            rawItems,
            getEvidence: item => item.Evidence.Select(e => (e.MessageId, e.Quote)).ToList(),
            getModelNeedsReview: item => item.NeedsReview,
            messagesById: messagesById);
    }

    private IReadOnlyList<RawWorkItem> ParseWorkItems(string? responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            _logger?.LogWarning("work_items extractor received an empty response.");
            return [];
        }

        try
        {
            var envelope = JsonSerializer.Deserialize<WorkItemsEnvelope>(responseText, JsonOptions);
            return envelope?.WorkItems ?? [];
        }
        catch (JsonException ex)
        {
            // A response that doesn't even parse as the schema's JSON shape can never be
            // grounded in evidence; fail closed to an empty item list rather than throwing
            // out of the whole extraction run.
            _logger?.LogWarning(ex, "work_items extractor response failed schema JSON parsing.");
            return [];
        }
    }

    private static string BuildUserPrompt(string template, ExtractionThread thread)
    {
        var scalars = new Dictionary<string, string>
        {
            ["project_name"] = thread.ProjectName ?? string.Empty,
            ["thread_subject"] = thread.Subject,
            ["today_iso"] = thread.Today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
        };

        var messageVars = thread.Messages
            .Select(m => (IReadOnlyDictionary<string, string>)new Dictionary<string, string>
            {
                ["id"] = m.Id,
                ["from"] = m.From,
                ["to"] = string.Join(", ", m.To),
                ["date"] = m.Date.ToString("O"),
                ["subject"] = m.Subject,
                ["datamarked_body"] = Spotlighting.Wrap(m.Id, Spotlighting.Datamark(m.CleanedBody)),
            })
            .ToList();

        return PromptRenderer.RenderUserPrompt(template, scalars, messageVars);
    }
}
