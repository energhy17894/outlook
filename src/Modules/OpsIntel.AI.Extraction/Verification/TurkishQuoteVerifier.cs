namespace OpsIntel.AI.Extraction.Verification;

/// <summary>Why a quote failed to verify. Mirrors <c>verify_quotes.py</c>'s <c>FailureReason</c> literal.</summary>
public enum QuoteFailureReason
{
    None,
    EmptyQuote,
    UnknownMessageId,
    NotFound,
}

/// <summary>
/// Result of verifying one <c>{message_id, quote}</c> evidence entry against its source
/// message's cleaned text. C# port of <c>tests/eval/verify_quotes.py</c>'s
/// <c>QuoteVerification</c> — same fields, same semantics, same char-offset contract (offsets
/// index into the ORIGINAL <c>source_text</c>, not the normalized text).
/// </summary>
public sealed record QuoteVerification(
    bool Found,
    string MessageId,
    int? CharStart,
    int? CharEnd,
    QuoteFailureReason FailureReason);

/// <summary>
/// Deterministic, Turkish-aware evidence quote verifier (ADR-0015's verification ladder step
/// 2: "alıntı, temizlenmiş metinde tr-TR normalizasyonuyla ... eşleşir"). This is the
/// production counterpart of <c>tests/eval/verify_quotes.py</c>, which remains the reference
/// implementation the eval harness uses; this class must match its behaviour bit-for-bit on
/// the shared test vectors (see <c>QuoteVerifierTestVectors</c>).
/// </summary>
public static class TurkishQuoteVerifier
{
    /// <summary>
    /// Verifies that <paramref name="quote"/> occurs verbatim (after Turkish-aware
    /// normalization) inside <paramref name="sourceText"/>. Returns char offsets into the
    /// ORIGINAL <paramref name="sourceText"/> on success. Uses a normalized exact-substring
    /// match, not fuzzy matching — the deterministic step of ADR-0015's verification ladder;
    /// an optional NLI/semantic step is a separate concern this method does not perform.
    /// </summary>
    public static QuoteVerification VerifyQuote(string? sourceText, string? quote, string messageId = "")
    {
        if (string.IsNullOrWhiteSpace(quote))
        {
            return new QuoteVerification(false, messageId, null, null, QuoteFailureReason.EmptyQuote);
        }

        var normSource = TurkishTextNormalizer.NormalizeWithMap(sourceText);
        var normQuote = TurkishTextNormalizer.NormalizeWithMap(quote);

        if (normQuote.Text.Length == 0)
        {
            return new QuoteVerification(false, messageId, null, null, QuoteFailureReason.EmptyQuote);
        }

        var pos = normSource.Text.IndexOf(normQuote.Text, StringComparison.Ordinal);
        if (pos < 0)
        {
            return new QuoteVerification(false, messageId, null, null, QuoteFailureReason.NotFound);
        }

        var (start, end) = normSource.ToOriginalSpan(pos, pos + normQuote.Text.Length);
        return new QuoteVerification(true, messageId, start, end, QuoteFailureReason.None);
    }

    /// <summary>
    /// Verifies each <c>{message_id, quote}</c> evidence entry against the matching source
    /// message's cleaned text. Returns one <see cref="QuoteVerification"/> per entry, in order.
    /// </summary>
    public static IReadOnlyList<QuoteVerification> VerifyEvidenceList(
        IReadOnlyList<(string MessageId, string Quote)> evidence,
        IReadOnlyDictionary<string, string> messagesById)
    {
        ArgumentNullException.ThrowIfNull(evidence);
        ArgumentNullException.ThrowIfNull(messagesById);

        var results = new List<QuoteVerification>(evidence.Count);
        foreach (var (messageId, quote) in evidence)
        {
            if (!messagesById.TryGetValue(messageId, out var sourceText))
            {
                results.Add(new QuoteVerification(false, messageId, null, null, QuoteFailureReason.UnknownMessageId));
                continue;
            }

            results.Add(VerifyQuote(sourceText, quote, messageId));
        }

        return results;
    }
}
