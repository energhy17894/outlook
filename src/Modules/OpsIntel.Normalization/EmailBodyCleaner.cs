using System.Text;
using System.Text.RegularExpressions;

namespace OpsIntel.Normalization;

/// <summary>
/// Result of cleaning a raw (HTML or plain-text) email body: the plain, quote/signature/
/// disclaimer-stripped text that extraction and quote verification operate on.
/// </summary>
/// <param name="CleanedText">
/// The cleaned plain text. This is the text every <c>Evidence.ExactQuote</c> char offset is
/// computed against (report §2 / AI notes §2: "store citations against the cleaned text").
/// </param>
/// <param name="RemovedQuotedReply">True if a trailing quoted-reply block was stripped.</param>
/// <param name="RemovedSignature">True if a trailing signature block was stripped.</param>
/// <param name="RemovedDisclaimer">True if a legal/confidentiality disclaimer was stripped.</param>
public sealed record CleanedBody(
    string CleanedText,
    bool RemovedQuotedReply,
    bool RemovedSignature,
    bool RemovedDisclaimer);

/// <summary>
/// Deterministic (Tier 0, per AI architecture notes §1) email body cleaner: HTML -&gt; text,
/// then TR/EN quoted-reply, signature and disclaimer stripping, so only the *new* text of a
/// message is passed to the extractor (AI notes §2, ADR-0015: own TR/EN stripper because
/// Graph <c>uniqueBody</c> can still contain the whole conversation).
/// </summary>
/// <remarks>
/// This is a best-effort, rule-based cleaner, not an HTML parser: it targets the common
/// Outlook/Exchange HTML shapes (blockquote reply chains, <c>&lt;div&gt;</c> based headers)
/// well enough for Faz 0. It intentionally never throws on malformed input — worst case is
/// that some quoted/signature text survives, which only makes the pipeline stricter (an
/// extractor still needs a verbatim evidence quote in whatever text is left).
/// </remarks>
public static class EmailBodyCleaner
{
    // --- HTML -> text ---------------------------------------------------------------

    private static readonly Regex ScriptOrStyle = new(
        @"<(script|style)\b[^>]*>.*?</\1>", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);

    private static readonly Regex BlockBreaks = new(
        @"</(p|div|br|tr|table|li|h[1-6])\s*/?>|<br\s*/?>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex AnyTag = new(@"<[^>]+>", RegexOptions.Compiled);

    private static readonly Regex WhitespaceRun = new(@"[ \t\x0B\f\r]+", RegexOptions.Compiled);

    private static readonly Regex BlankLineRun = new(@"\n{3,}", RegexOptions.Compiled);

    /// <summary>Converts HTML email body markup to plain text (block-level tags become newlines).</summary>
    public static string HtmlToText(string html)
    {
        if (string.IsNullOrEmpty(html))
        {
            return string.Empty;
        }

        var text = ScriptOrStyle.Replace(html, string.Empty);
        text = BlockBreaks.Replace(text, "\n");
        text = AnyTag.Replace(text, string.Empty);
        text = System.Net.WebUtility.HtmlDecode(text);
        text = WhitespaceRun.Replace(text, " ");
        text = BlankLineRun.Replace(text, "\n\n");
        return text.Trim();
    }

    // --- Quoted-reply header patterns (TR + EN) --------------------------------------

    // "From:"/"Kimden:" ... "Sent:"/"Gönderildi:" style headers used by Outlook when it
    // inlines the previous message as plain text (not a blockquote).
    private static readonly Regex OutlookHeaderBlock = new(
        @"^[ \t]*(From|Kimden)\s*:.*$", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    // "On <date>, <name> wrote:" / "<name> tarihinde ... yazdı:" reply-attribution lines.
    private static readonly Regex OnWroteLine = new(
        @"^[ \t]*On\s.{0,200}?\swrote:\s*$",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    private static readonly Regex TarihindeYazdiLine = new(
        @"^[ \t]*.{0,200}?\starihinde.{0,200}?\byazdı\s*:\s*$",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    private static readonly Regex OriginalMessageLine = new(
        @"^[ \t]*-{2,}\s*(Original Message|Orijinal Mesaj|İletilen Mesaj|Forwarded Message)\s*-{2,}\s*$",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase);

    // Lines starting with '>' (classic plain-text quoting).
    private static readonly Regex QuoteMarkerRunStart = new(
        @"^[ \t]*>", RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>
    /// Strips a trailing quoted-reply block. Looks for the earliest quoted-reply marker
    /// (Outlook "From:/Kimden:" header block, "On ... wrote:", "... tarihinde ... yazdı:",
    /// "-----Original Message-----", or a run of "&gt;"-quoted lines) and truncates
    /// everything from that point on, since it is the *previous* message's text, not this
    /// message's own content.
    /// </summary>
    public static (string Text, bool Removed) StripQuotedReply(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return (text, false);
        }

        var earliest = int.MaxValue;

        foreach (Match m in OutlookHeaderBlock.Matches(text))
        {
            if (LooksLikeReplyHeaderBlock(text, m.Index))
            {
                earliest = Math.Min(earliest, m.Index);
                break;
            }
        }

        TryEarliest(OnWroteLine, text, ref earliest);
        TryEarliest(TarihindeYazdiLine, text, ref earliest);
        TryEarliest(OriginalMessageLine, text, ref earliest);

        var firstQuoteLine = QuoteMarkerRunStart.Match(text);
        if (firstQuoteLine.Success)
        {
            earliest = Math.Min(earliest, firstQuoteLine.Index);
        }

        if (earliest == int.MaxValue)
        {
            return (text, false);
        }

        var kept = text[..earliest].TrimEnd();
        return (kept, kept.Length < text.TrimEnd().Length);
    }

    private static void TryEarliest(Regex regex, string text, ref int earliest)
    {
        var m = regex.Match(text);
        if (m.Success)
        {
            earliest = Math.Min(earliest, m.Index);
        }
    }

    // A "From:/Kimden:" line is only treated as a reply header if within the next few lines
    // a "Sent:/Gönderildi:" or "To:/Kime:" line also appears (avoids false positives on a
    // message that merely opens with "From: <person>," as prose).
    private static bool LooksLikeReplyHeaderBlock(string text, int fromLineIndex)
    {
        var window = text.Substring(fromLineIndex, Math.Min(400, text.Length - fromLineIndex));
        return Regex.IsMatch(window, @"^[ \t]*(Sent|Gönderildi|To|Kime)\s*:", RegexOptions.Multiline | RegexOptions.IgnoreCase);
    }

    // --- Signature blocks --------------------------------------------------------------

    // Standard Usenet/email signature delimiter.
    private static readonly Regex SigDelimiter = new(@"^--\s*$", RegexOptions.Compiled | RegexOptions.Multiline);

    // TR/EN valedictions that typically open the signature block when there is no "--" delimiter.
    private static readonly string[] Valedictions =
    [
        "saygılarımla", "saygılar", "iyi çalışmalar", "iyi günler", "teşekkürler",
        "kind regards", "best regards", "regards", "sincerely", "thanks", "thank you", "warm regards",
    ];

    /// <summary>
    /// Strips a trailing signature block: everything after a "--" delimiter line, or (if
    /// none) everything after a short trailing valediction line ("Saygılarımla," / "Best
    /// regards,") followed by a short name/title/contact block, mirroring how TR/EN
    /// corporate signatures are structured (name, title, company, phone).
    /// </summary>
    public static (string Text, bool Removed) StripSignature(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return (text, false);
        }

        var delim = SigDelimiter.Match(text);
        if (delim.Success)
        {
            var kept = text[..delim.Index].TrimEnd();
            return (kept, true);
        }

        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            // Turkish-aware fold for the valediction match only: plain ToLowerInvariant()
            // leaves 'İ' (U+0130) uppercase (it is not part of the invariant lowering table),
            // which would silently fail to match "iyi çalışmalar" etc.
            var line = lines[i].Trim().TrimEnd(',', '.', ':')
                .Replace('İ', 'i').Replace('I', 'ı')
                .ToLowerInvariant();
            if (line.Length == 0 || line.Length > 40)
            {
                continue;
            }

            if (Array.Exists(Valedictions, v => line == v || line.StartsWith(v, StringComparison.Ordinal)))
            {
                // Only treat as a signature opener if it's within the trailing ~40% of the
                // message and is followed by at most a handful of short lines (name/title/
                // company/phone), never by another paragraph of real prose.
                var remaining = lines.Length - i - 1;
                if (i < lines.Length * 0.3)
                {
                    continue;
                }

                if (remaining > 6)
                {
                    continue;
                }

                var kept = string.Join('\n', lines[..i]).TrimEnd();
                return (kept, kept.Length > 0);
            }
        }

        return (text, false);
    }

    // --- Legal / confidentiality disclaimers --------------------------------------------

    private static readonly Regex[] DisclaimerStarts =
    [
        new(@"^[ \t]*Bu\s+e-?posta(\s+ve\s+ekleri)?\s+gizli", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase),
        new(@"^[ \t]*This\s+e-?mail(\s+and\s+any\s+attachments)?\s+(is|are|may\s+be)?\s*confidential", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase),
        new(@"^[ \t]*This\s+message\s+(is|contains)\s+confidential", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase),
        new(@"^[ \t]*Confidentiality\s+Notice", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase),
        new(@"^[ \t]*Gizlilik\s+(Uyarısı|Bildirimi)", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase),
    ];

    /// <summary>
    /// Strips a trailing legal/confidentiality disclaimer paragraph ("Bu e-posta ve ekleri
    /// gizlidir…" / "This email and any attachments is confidential…").
    /// </summary>
    public static (string Text, bool Removed) StripDisclaimer(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return (text, false);
        }

        var earliest = int.MaxValue;
        foreach (var pattern in DisclaimerStarts)
        {
            var m = pattern.Match(text);
            if (m.Success)
            {
                earliest = Math.Min(earliest, m.Index);
            }
        }

        if (earliest == int.MaxValue)
        {
            return (text, false);
        }

        var kept = text[..earliest].TrimEnd();
        return (kept, true);
    }

    /// <summary>
    /// Full pipeline: optional HTML-&gt;text, then quoted-reply, signature and disclaimer
    /// stripping (in that order — a disclaimer sometimes trails a signature, a signature
    /// always precedes a quoted reply chain in Outlook's own composition order... but since
    /// stripping is idempotent-truncating, order only matters for which strip "wins" the
    /// earliest cut point, so we simply re-run until no further reduction is achieved to be
    /// order-independent).
    /// </summary>
    public static CleanedBody Clean(string? rawBody, bool isHtml)
    {
        var text = rawBody ?? string.Empty;
        if (isHtml)
        {
            text = HtmlToText(text);
        }

        text = NormalizeLineEndings(text);

        var removedQuote = false;
        var removedSig = false;
        var removedDisclaimer = false;

        // Iterate to a fixed point: each strip only ever truncates, so this terminates in
        // at most a few passes and is insensitive to which pattern fires first.
        for (var i = 0; i < 4; i++)
        {
            var before = text;

            var (afterQuote, q) = StripQuotedReply(text);
            text = afterQuote;
            removedQuote |= q;

            var (afterSig, s) = StripSignature(text);
            text = afterSig;
            removedSig |= s;

            var (afterDisc, d) = StripDisclaimer(text);
            text = afterDisc;
            removedDisclaimer |= d;

            if (text == before)
            {
                break;
            }
        }

        text = BlankLineRun.Replace(text, "\n\n").Trim();

        return new CleanedBody(text, removedQuote, removedSig, removedDisclaimer);
    }

    private static string NormalizeLineEndings(string text)
    {
        return text.Replace("\r\n", "\n").Replace('\r', '\n');
    }
}
