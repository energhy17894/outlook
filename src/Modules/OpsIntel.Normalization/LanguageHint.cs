using System.Text.RegularExpressions;

namespace OpsIntel.Normalization;

/// <summary>Two-letter language hint the pipeline is prepared to handle (AI notes §2: "mixed TR/EN is common").</summary>
public enum LanguageCode
{
    Unknown,
    Tr,
    En,
}

/// <summary>
/// Cheap, dependency-free per-paragraph/per-message language heuristic (Tier 0, AI notes
/// §1/§2). Not a real language-id model: it is a scored heuristic over Turkish-specific
/// letters and a short TR/EN stopword list, good enough to route "mostly Turkish" vs
/// "mostly English" text for prompt/date-locale decisions. A thread may mix both; call this
/// per message or per paragraph rather than once for a whole thread.
/// </summary>
public static class LanguageHint
{
    private static readonly char[] TurkishOnlyLetters = ['ç', 'ğ', 'ı', 'ö', 'ş', 'ü', 'Ç', 'Ğ', 'İ', 'Ö', 'Ş', 'Ü'];

    private static readonly string[] TurkishStopwords =
    [
        "ve", "bir", "bu", "için", "ile", "de", "da", "ki", "gibi", "çok", "ama", "veya",
        "değil", "olan", "olarak", "kadar", "sonra", "önce", "mı", "mi", "mu", "mü", "lütfen",
        "teşekkürler", "merhaba", "selamlar", "rica", "ederim", "tarihinde", "yazdı",
    ];

    private static readonly string[] EnglishStopwords =
    [
        "the", "and", "to", "of", "a", "in", "is", "that", "it", "for", "on", "with", "as",
        "are", "was", "please", "thanks", "regards", "hello", "hi", "will", "would",
    ];

    private static readonly Regex WordSplit = new(@"[^\p{L}]+", RegexOptions.Compiled);

    /// <summary>Scores the text and returns the best-guess language, or <see cref="LanguageCode.Unknown"/> for very short/ambiguous input.</summary>
    public static LanguageCode Detect(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return LanguageCode.Unknown;
        }

        var trScore = 0.0;
        var enScore = 0.0;

        foreach (var ch in text)
        {
            if (Array.IndexOf(TurkishOnlyLetters, ch) >= 0)
            {
                trScore += 3;
            }
        }

        var words = WordSplit.Split(text.ToLowerInvariant());
        var totalWords = 0;
        foreach (var word in words)
        {
            if (word.Length == 0)
            {
                continue;
            }

            totalWords++;
            if (Array.IndexOf(TurkishStopwords, word) >= 0)
            {
                trScore += 1;
            }

            if (Array.IndexOf(EnglishStopwords, word) >= 0)
            {
                enScore += 1;
            }
        }

        if (totalWords < 3 && trScore == 0 && enScore == 0)
        {
            return LanguageCode.Unknown;
        }

        if (trScore == 0 && enScore == 0)
        {
            return LanguageCode.Unknown;
        }

        return trScore >= enScore ? LanguageCode.Tr : LanguageCode.En;
    }
}
