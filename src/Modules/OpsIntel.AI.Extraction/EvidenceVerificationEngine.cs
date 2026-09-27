using OpsIntel.AI.Extraction.Verification;

namespace OpsIntel.AI.Extraction;

/// <summary>
/// Shared deterministic verification step (ADR-0015 / ADR-0019) applied after every schema
/// extractor call, regardless of which schema (<c>work_items</c>, <c>decisions</c>,
/// <c>risks</c>, ...) produced the raw items: verify each item's evidence quotes against the
/// cleaned source text, drop items with no verifiable evidence at all, and mark the rest
/// <c>NeedsReview</c> whenever any of their claimed evidence failed to verify.
/// </summary>
public static class EvidenceVerificationEngine
{
    /// <summary>
    /// Verifies <paramref name="rawItems"/> and returns the surviving items with their
    /// evidence trimmed down to only what verified.
    /// </summary>
    /// <param name="rawItems">The raw, schema-shaped items returned by the model.</param>
    /// <param name="getEvidence">Projects an item's raw <c>{message_id, quote}</c> evidence list.</param>
    /// <param name="getModelNeedsReview">Projects the model's own <c>needs_review</c> flag for an item.</param>
    /// <param name="messagesById">Cleaned message text by message id, for quote verification.</param>
    public static ExtractionOutcome<TItem> Verify<TItem>(
        IReadOnlyList<TItem> rawItems,
        Func<TItem, IReadOnlyList<(string MessageId, string Quote)>> getEvidence,
        Func<TItem, bool> getModelNeedsReview,
        IReadOnlyDictionary<string, string> messagesById)
    {
        ArgumentNullException.ThrowIfNull(rawItems);
        ArgumentNullException.ThrowIfNull(getEvidence);
        ArgumentNullException.ThrowIfNull(getModelNeedsReview);
        ArgumentNullException.ThrowIfNull(messagesById);

        var kept = new List<VerifiedItem<TItem>>();
        var dropped = 0;

        foreach (var item in rawItems)
        {
            var evidence = getEvidence(item);
            var verifiedEvidence = new List<VerifiedEvidence>(evidence.Count);
            var anyFailed = false;

            foreach (var (messageId, quote) in evidence)
            {
                messagesById.TryGetValue(messageId, out var sourceText);
                var result = TurkishQuoteVerifier.VerifyQuote(sourceText, quote, messageId);

                if (sourceText is null)
                {
                    result = new QuoteVerification(false, messageId, null, null, QuoteFailureReason.UnknownMessageId);
                }

                verifiedEvidence.Add(new VerifiedEvidence(
                    messageId,
                    quote,
                    result.Found,
                    result.CharStart,
                    result.CharEnd,
                    result.FailureReason));

                if (!result.Found)
                {
                    anyFailed = true;
                }
            }

            var survivors = verifiedEvidence.Where(e => e.Verified).ToList();
            if (survivors.Count == 0)
            {
                // No verifiable evidence at all: drop entirely (ADR-0015 — never shown as fact).
                dropped++;
                continue;
            }

            var needsReview = getModelNeedsReview(item) || anyFailed;
            kept.Add(new VerifiedItem<TItem>(item, survivors, needsReview));
        }

        return new ExtractionOutcome<TItem>(kept, dropped);
    }
}
