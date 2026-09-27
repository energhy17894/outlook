using System.Text.Json.Serialization;

namespace OpsIntel.AI.Extraction.WorkItems;

/// <summary>One raw <c>{message_id, quote}</c> evidence entry as returned by the model, before verification.</summary>
public sealed record RawEvidence(
    [property: JsonPropertyName("message_id")] string MessageId,
    [property: JsonPropertyName("quote")] string Quote);

/// <summary>
/// One <c>work_items[]</c> entry, deserialized straight from the model's structured JSON
/// output. Field names/shape mirror <c>prompts/extraction/v1/schemas/work_items.schema.json</c>
/// exactly (do not add/rename fields without updating the schema and vice versa).
/// </summary>
public sealed record RawWorkItem(
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("owner_email")] string? OwnerEmail,
    [property: JsonPropertyName("counterparty_email")] string? CounterpartyEmail,
    [property: JsonPropertyName("due_date")] string? DueDate,
    [property: JsonPropertyName("due_date_text")] string? DueDateText,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("confidence")] string Confidence,
    [property: JsonPropertyName("needs_review")] bool NeedsReview,
    [property: JsonPropertyName("abstain_reason")] string? AbstainReason,
    [property: JsonPropertyName("evidence")] IReadOnlyList<RawEvidence> Evidence);

/// <summary>The <c>work_items</c> schema's top-level envelope: <c>{"work_items": [...]}</c>.</summary>
public sealed record WorkItemsEnvelope(
    [property: JsonPropertyName("work_items")] IReadOnlyList<RawWorkItem> WorkItems);
