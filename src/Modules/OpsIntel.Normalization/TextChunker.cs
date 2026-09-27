namespace OpsIntel.Normalization;

/// <summary>A chunk of cleaned text with a stable char span into that same cleaned text (AI notes §2: "every chunk carries ... char offsets").</summary>
public sealed record TextChunk(int CharStart, int CharEnd, string Text);

/// <summary>
/// Splits cleaned message text into paragraph-sized chunks, merging short paragraphs and
/// splitting overlong ones, per AI notes §2 ("one unit per message, split to paragraphs if
/// long"). Offsets are into the exact string passed in, so callers can map a chunk straight
/// back onto the message's cleaned text (and, via the cleaner's own bookkeeping, onward to
/// the raw text) without re-scanning.
/// </summary>
public static class TextChunker
{
    /// <summary>
    /// Chunks <paramref name="cleanedText"/> into paragraphs, merging adjacent paragraphs
    /// while the running chunk stays under <paramref name="targetMaxChars"/>, and hard-splitting
    /// a single paragraph that alone exceeds it.
    /// </summary>
    public static IReadOnlyList<TextChunk> Chunk(string cleanedText, int targetMaxChars = 2000)
    {
        ArgumentNullException.ThrowIfNull(cleanedText);
        if (targetMaxChars <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetMaxChars));
        }

        if (cleanedText.Length == 0)
        {
            return [];
        }

        var paragraphs = SplitParagraphs(cleanedText);
        var chunks = new List<TextChunk>();

        var runStart = -1;
        var runEnd = -1;

        void FlushRun()
        {
            if (runStart < 0)
            {
                return;
            }

            chunks.Add(new TextChunk(runStart, runEnd, cleanedText[runStart..runEnd]));
            runStart = -1;
            runEnd = -1;
        }

        foreach (var (start, end) in paragraphs)
        {
            var paragraphLength = end - start;

            if (paragraphLength > targetMaxChars)
            {
                FlushRun();

                var offset = start;
                while (offset < end)
                {
                    var take = Math.Min(targetMaxChars, end - offset);
                    chunks.Add(new TextChunk(offset, offset + take, cleanedText[offset..(offset + take)]));
                    offset += take;
                }

                continue;
            }

            if (runStart < 0)
            {
                runStart = start;
                runEnd = end;
                continue;
            }

            if (end - runStart <= targetMaxChars)
            {
                runEnd = end;
            }
            else
            {
                FlushRun();
                runStart = start;
                runEnd = end;
            }
        }

        FlushRun();
        return chunks;
    }

    private static List<(int Start, int End)> SplitParagraphs(string text)
    {
        var result = new List<(int, int)>();
        var i = 0;
        var n = text.Length;

        while (i < n)
        {
            // Skip leading blank lines / whitespace-only runs.
            while (i < n && IsBlankAt(text, i, out var advance))
            {
                i += advance;
            }

            if (i >= n)
            {
                break;
            }

            var start = i;
            var end = n;

            var searchFrom = i;
            while (searchFrom < n)
            {
                var nlnl = text.IndexOf("\n\n", searchFrom, StringComparison.Ordinal);
                if (nlnl < 0)
                {
                    end = n;
                    break;
                }

                end = nlnl;
                searchFrom = nlnl + 2;
                break;
            }

            var trimmedEnd = end;
            while (trimmedEnd > start && char.IsWhiteSpace(text[trimmedEnd - 1]))
            {
                trimmedEnd--;
            }

            if (trimmedEnd > start)
            {
                result.Add((start, trimmedEnd));
            }

            i = end < n ? end + 2 : n;
        }

        return result;
    }

    private static bool IsBlankAt(string text, int index, out int advance)
    {
        if (text[index] is '\n' or '\r' or ' ' or '\t')
        {
            advance = 1;
            return true;
        }

        advance = 0;
        return false;
    }
}
