namespace OpsIntel.AI.Extraction;

/// <summary>
/// One cleaned message in a thread, as handed to an extractor. <see cref="CleanedBody"/> is
/// the text produced by <c>OpsIntel.Normalization.EmailBodyCleaner</c> — the same text
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
