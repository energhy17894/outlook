namespace OpsIntel.Contracts;

/// <summary>
/// An evidence anchor binding an extracted claim to an exact location in a source message or
/// document, modeled after the W3C Web Annotation <c>TextQuoteSelector</c> (exact quote +
/// prefix/suffix) and <c>TextPositionSelector</c> (character offsets) — see report §3. Nothing
/// in OpsIntel is ever stored as free text without one of these.
/// </summary>
public sealed record Evidence(
    string Id,
    string WorkItemId,
    string RawItemId,
    string ExactQuote,
    string? Prefix,
    string? Suffix,
    int CharStart,
    int CharEnd,
    DateTimeOffset SourceTimestampUtc,
    string? AuthorPersonId,
    bool Verified,
    string? VerifierPersonId);
