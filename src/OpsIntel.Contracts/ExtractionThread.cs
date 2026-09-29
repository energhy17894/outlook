namespace OpsIntel.Contracts;

/// <summary>
/// Job types on the shared job queue (ADR-0012) that carry an <see cref="ExtractionThread"/>
/// payload. Lives here so the Host (producer) and Intelligence (consumer) agree on the string
/// without either referencing the other.
/// </summary>
public static class ExtractionJobTypes
{
    /// <summary>Work-item extraction; payload is a JSON-serialized <see cref="ExtractionThread"/>.</summary>
    public const string WorkItems = "work_items_extraction";
}

/// <summary>
/// One cleaned message in a thread, as handed to an extractor. <see cref="CleanedBody"/> is
/// the text produced by <c>EmailBodyCleaner</c> — the same text
/// evidence quotes are verified against.
/// </summary>
public sealed record ExtractionMessage(
    string Id,
    string From,
    IReadOnlyList<string> To,
    DateTimeOffset Date,
    string Subject,
    string CleanedBody);

/// <summary>The input to a schema extractor: one thread's messages in chronological order, plus prompt context.</summary>
public sealed record ExtractionThread(
    string ThreadId,
    string Subject,
    string? ProjectName,
    DateOnly Today,
    IReadOnlyList<ExtractionMessage> Messages);
