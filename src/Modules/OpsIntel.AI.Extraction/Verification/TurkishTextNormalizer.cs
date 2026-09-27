using System.Globalization;
using System.Text;

namespace OpsIntel.AI.Extraction.Verification;

/// <summary>
/// Normalized text plus a map from each normalized-char index back to the original string's
/// char index it was derived from, so a match in normalized space can be turned back into a
/// <c>(char_start, char_end)</c> span in the original text (the <see cref="Evidence"/>
/// <c>TextPositionSelector</c>).
/// </summary>
public sealed class NormalizedText
{
    public static readonly NormalizedText Empty = new(string.Empty, []);

    public NormalizedText(string text, IReadOnlyList<int> indexMap)
    {
        Text = text;
        IndexMap = indexMap;
    }

    public string Text { get; }

    public IReadOnlyList<int> IndexMap { get; }

    /// <summary>
    /// <paramref name="normEnd"/> is exclusive; returns an exclusive (start, end) span in the
    /// original text that fully covers the matched normalized range.
    /// </summary>
    public (int Start, int End) ToOriginalSpan(int normStart, int normEnd)
    {
        if (normEnd <= normStart)
        {
            throw new ArgumentOutOfRangeException(nameof(normEnd), "normEnd must be greater than normStart.");
        }

        var start = IndexMap[normStart];
        var end = IndexMap[normEnd - 1] + 1;
        return (start, end);
    }
}

/// <summary>
/// C# port of <c>tests/eval/normalize.py</c> (the reference implementation). Must match its
/// normalization behaviour exactly: Unicode NFC of the whole string, Turkish-aware casefold
/// (İ/ı/I/i handled explicitly, since neither <see cref="string.ToLowerInvariant"/> nor
/// <c>string.ToLower(CultureInfo.GetCultureInfo("tr-TR"))</c> reproduces Python's
/// <c>str.casefold()</c> bug-for-bug on <c>İ</c> the way the Python reference deliberately
/// avoids it), quote/dash/NBSP folding, then whitespace-run collapse — each stage tracked
/// back to the original string so callers get correct char offsets.
/// </summary>
public static class TurkishTextNormalizer
{
    // Turkish-specific casing exceptions plain casing rules get wrong (see normalize.py docstring).
    private static readonly Dictionary<char, char> TurkishUpperToLower = new()
    {
        ['İ'] = 'i', // U+0130 dotted capital I -> plain i (NOT i + combining dot above)
        ['I'] = 'ı', // U+0049 undotted capital I -> dotless ı (U+0131)
    };

    private static readonly Dictionary<char, char> PunctFold = new()
    {
        ['’'] = '\'', // RIGHT SINGLE QUOTATION MARK
        ['‘'] = '\'', // LEFT SINGLE QUOTATION MARK
        ['“'] = '"', // LEFT DOUBLE QUOTATION MARK
        ['”'] = '"', // RIGHT DOUBLE QUOTATION MARK
        ['–'] = '-', // EN DASH
        ['—'] = '-', // EM DASH
        [' '] = ' ', // NO-BREAK SPACE
    };

    /// <summary>Normalizes text and returns a per-character provenance map (see <see cref="NormalizedText.ToOriginalSpan"/>).</summary>
    public static NormalizedText NormalizeWithMap(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return NormalizedText.Empty;
        }

        var (nfcText, nfcOrigin) = NfcAlign(text);

        var foldedChars = new StringBuilder(nfcText.Length);
        var foldedOrigin = new List<int>(nfcText.Length);
        for (var i = 0; i < nfcText.Length; i++)
        {
            var origin = nfcOrigin[i];
            var folded = FoldChar(nfcText[i]);
            foreach (var fc in folded)
            {
                foldedChars.Append(fc);
                foldedOrigin.Add(origin);
            }
        }

        var outChars = new StringBuilder(foldedChars.Length);
        var outOrigin = new List<int>(foldedChars.Length);
        var prevWasSpace = true; // true at start so leading whitespace is dropped

        for (var i = 0; i < foldedChars.Length; i++)
        {
            var ch = foldedChars[i];
            var origin = foldedOrigin[i];
            if (char.IsWhiteSpace(ch))
            {
                if (prevWasSpace)
                {
                    continue;
                }

                outChars.Append(' ');
                outOrigin.Add(origin);
                prevWasSpace = true;
            }
            else
            {
                outChars.Append(ch);
                outOrigin.Add(origin);
                prevWasSpace = false;
            }
        }

        while (outChars.Length > 0 && outChars[^1] == ' ')
        {
            outChars.Length--;
            outOrigin.RemoveAt(outOrigin.Count - 1);
        }

        return new NormalizedText(outChars.ToString(), outOrigin);
    }

    /// <summary>Convenience wrapper for callers that only need the normalized string, not the offset map.</summary>
    public static string NormalizeForCompare(string? text) => NormalizeWithMap(text).Text;

    private static string FoldChar(char ch)
    {
        if (TurkishUpperToLower.TryGetValue(ch, out var mapped))
        {
            return mapped.ToString();
        }

        if (PunctFold.TryGetValue(ch, out var punct))
        {
            return punct.ToString();
        }

        // char.ToLowerInvariant does not casefold-expand (e.g. German ß -> "ss") the way
        // Python's str.casefold() can; that divergence is out of scope for the Turkish
        // business-email corpus this verifier targets.
        return char.ToLowerInvariant(ch).ToString();
    }

    /// <summary>
    /// NFC-normalizes the whole string and returns (nfcText, indexMap) where
    /// indexMap[i] is the original-string index the nfcText[i] character is derived from.
    /// Fast path: if NFC doesn't change anything (the overwhelmingly common case), the map is
    /// the identity map. Otherwise falls back to an LCS-based alignment mirroring the Python
    /// reference's difflib-based approach.
    /// </summary>
    private static (string NfcText, IReadOnlyList<int> IndexMap) NfcAlign(string text)
    {
        var nfc = text.Normalize(NormalizationForm.FormC);
        if (nfc == text)
        {
            var identity = new int[nfc.Length];
            for (var i = 0; i < identity.Length; i++)
            {
                identity[i] = i;
            }

            return (nfc, identity);
        }

        return (nfc, LcsAlign(text, nfc));
    }

    /// <summary>
    /// Aligns <paramref name="nfc"/> back onto <paramref name="original"/> via an LCS
    /// backtrack: characters shared between the two (in order) map 1:1; a run of
    /// nfc-only characters (produced by composition) all map to the same origin index in
    /// <paramref name="original"/> (the position right after the last matched original
    /// character, clamped to the last valid index), mirroring normalize.py's opcode-based
    /// index_map construction.
    /// </summary>
    private static int[] LcsAlign(string original, string nfc)
    {
        var n = original.Length;
        var m = nfc.Length;

        // dp[i][j] = length of LCS of original[i..] and nfc[j..]
        var dp = new int[n + 1, m + 1];
        for (var i = n - 1; i >= 0; i--)
        {
            for (var j = m - 1; j >= 0; j--)
            {
                dp[i, j] = original[i] == nfc[j]
                    ? dp[i + 1, j + 1] + 1
                    : Math.Max(dp[i + 1, j], dp[i, j + 1]);
            }
        }

        var indexMap = new int[m];
        var oi = 0;
        var nj = 0;
        while (nj < m)
        {
            if (oi < n && original[oi] == nfc[nj] && dp[oi, nj] == dp[oi + 1, nj + 1] + 1)
            {
                indexMap[nj] = oi;
                oi++;
                nj++;
            }
            else if (oi < n && dp[oi + 1, nj] >= dp[oi, nj + 1])
            {
                // original[oi] is consumed (decomposed away by composition) without
                // producing an nfc character; advance the origin cursor only.
                oi++;
            }
            else
            {
                // nfc[nj] has no direct original counterpart (a composed character):
                // attribute it to the current origin cursor, clamped into range.
                indexMap[nj] = Math.Min(oi, Math.Max(0, n - 1));
                nj++;
            }
        }

        return indexMap;
    }
}
