using OpsIntel.AI.Extraction.Verification;

namespace OpsIntel.AI.Extraction;

/// <summary>One evidence entry after local deterministic verification (ADR-0015's verification ladder step 2).</summary>
public sealed record VerifiedEvidence(
    string MessageId,
    string Quote,
    bool Verified,
    int? CharStart,
    int? CharEnd,
    QuoteFailureReason FailureReason);

/// <summary>
/// One extracted item after evidence verification: the raw model output plus only its
/// verified evidence quotes and the post-verification review flag.
/// </summary>
/// <typeparam name="TItem">The raw, schema-shaped item type (e.g. <c>RawWorkItem</c>).</typeparam>
/// <param name="Source">The original model output for this item.</param>
/// <param name="VerifiedEvidence">
/// Only the evidence entries that verified against their source message's cleaned text
/// (unverified entries are dropped, never surfaced as if they were real).
/// </param>
/// <param name="NeedsReview">
/// True if the model itself asked for review, or if any of the model's evidence entries
/// failed verification (i.e. the item is shown with a strict subset of its claimed evidence).
/// </param>
public sealed record VerifiedItem<TItem>(
    TItem Source,
    IReadOnlyList<VerifiedEvidence> VerifiedEvidence,
    bool NeedsReview);

/// <summary>
/// The result of one extractor call: items that had at least one verifiable evidence quote.
/// Items with zero verifiable evidence are dropped entirely per ADR-0015 ("doğrulanamayan öğe
/// asla 'gerçek' olarak gösterilmez") — never surfaced, not even as <c>NeedsReview</c>, since
/// there is nothing left to anchor them to.
/// </summary>
public sealed record ExtractionOutcome<TItem>(
    IReadOnlyList<VerifiedItem<TItem>> Items,
    int DroppedForNoVerifiableEvidence);
