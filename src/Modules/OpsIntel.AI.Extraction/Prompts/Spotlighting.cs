using OpsIntel.Contracts;
using System.Text.RegularExpressions;

namespace OpsIntel.AI.Extraction.Prompts;

/// <summary>
/// C# implementation of the delimiting + datamarking convention documented in
/// <c>prompts/extraction/v1/_shared/spotlighting.md</c> (Hines et al., ADR-0019). Every
/// untrusted message body handed to the extractor is wrapped in a matched
/// <c>&lt;&lt;&lt;UNTRUSTED_EMAIL id="..."&gt;&gt;&gt; ... &lt;&lt;&lt;END_UNTRUSTED_EMAIL id="..."&gt;&gt;&gt;</c>
/// block, and whitespace inside the block is replaced with the datamarking character
/// U+2423 (OPEN BOX) so untrusted spans read as visibly data-shaped to the model.
/// </summary>
/// <remarks>
/// Quote verification always runs against the pipeline's own cleaned text (never the
/// datamarked text) — <see cref="Verification.TurkishQuoteVerifier"/> is handed
/// <see cref="ExtractionMessage.CleanedBody"/> directly, so a model that echoes back the
/// datamarked form as its evidence quote will simply fail verification (the marker is not
/// stripped back to a space, matching the "verifier strips the marker" note in
/// spotlighting.md only insofar as the model is expected to quote the *clean* form; a model
/// quoting datamarked text is treated as an unverifiable quote, which is the safe failure
/// mode: the item is flagged <c>NeedsReview</c> rather than silently accepted).
/// </remarks>
public static class Spotlighting
{
    public const char DatamarkChar = '␣';

    private static readonly Regex WhitespaceRun = new(@"\s+", RegexOptions.Compiled);

    /// <summary>Replaces every run of whitespace with <see cref="DatamarkChar"/>.</summary>
    public static string Datamark(string text) => WhitespaceRun.Replace(text ?? string.Empty, DatamarkChar.ToString());

    /// <summary>Wraps an already-datamarked body in the untrusted-content delimiter pair.</summary>
    public static string Wrap(string messageId, string datamarkedBody)
        => $"<<<UNTRUSTED_EMAIL id=\"{messageId}\">>>\n{datamarkedBody}\n<<<END_UNTRUSTED_EMAIL id=\"{messageId}\">>>";
}
