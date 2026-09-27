using OpsIntel.AI.Extraction.Verification;
using Xunit;

namespace OpsIntel.AI.Tests;

/// <summary>
/// C# port of <c>tests/eval/tests/test_verify_quotes.py</c>'s test vectors, so the Turkish
/// quote verifier stays behaviorally identical to the Python reference implementation the
/// eval harness uses (ADR-0015: "production .NET implementation must match its
/// normalization behaviour"). Test names and source strings are kept 1:1 with the Python
/// file where practical.
/// </summary>
public sealed class TurkishQuoteVerifierTests
{
    [Fact]
    public void ExactMatchAscii()
    {
        var source = "We will ship the report by Friday.";
        var result = TurkishQuoteVerifier.VerifyQuote(source, "ship the report by Friday");

        Assert.True(result.Found);
        Assert.Equal("ship the report by Friday", source[result.CharStart!.Value..result.CharEnd!.Value]);
    }

    [Fact]
    public void NotFound()
    {
        var result = TurkishQuoteVerifier.VerifyQuote("Hello world", "goodbye world");

        Assert.False(result.Found);
        Assert.Equal(QuoteFailureReason.NotFound, result.FailureReason);
    }

    [Fact]
    public void EmptyQuoteRejected()
    {
        var result = TurkishQuoteVerifier.VerifyQuote("Some text here", "   ");

        Assert.False(result.Found);
        Assert.Equal(QuoteFailureReason.EmptyQuote, result.FailureReason);
    }

    [Fact]
    public void TurkishDottedCapitalIFoldsToDottedLowercaseI()
    {
        // 'İ' (U+0130) must fold to plain 'i', not to 'i' + combining dot above.
        var source = "İYİ ÇALIŞMALAR, teklif İptal edildi.";
        var quote = "teklif iptal edildi";

        var result = TurkishQuoteVerifier.VerifyQuote(source, quote);

        Assert.True(result.Found);
        Assert.Equal("teklif İptal edildi", source[result.CharStart!.Value..result.CharEnd!.Value]);
    }

    [Fact]
    public void TurkishUndottedCapitalIFoldsToDotlessI()
    {
        // 'I' (U+0049) must fold to 'ı' (U+0131) under Turkish rules, not to plain 'i'.
        var source = "PROJE IŞIK durumu güncellendi.";
        var quote = "proje ışık durumu";

        var result = TurkishQuoteVerifier.VerifyQuote(source, quote);

        Assert.True(result.Found);
    }

    [Fact]
    public void TurkishNaiveCasefoldWouldHaveFailed()
    {
        // Demonstrates the exact bug this module exists to avoid: naive lowering can turn
        // 'İ' into something other than plain 'i'; the Turkish-aware normalizer must not.
        Assert.Equal("i", TurkishTextNormalizer.NormalizeForCompare("İ"));
    }

    [Fact]
    public void LowercaseIAndDotlessIAreDistinctAfterNormalization()
    {
        Assert.NotEqual(
            TurkishTextNormalizer.NormalizeForCompare("ışık"),
            TurkishTextNormalizer.NormalizeForCompare("isik"));
    }

    [Fact]
    public void WhitespaceCollapseAndNewlines()
    {
        var source = "Merhaba,\n\n   Bütçe   revizyonunu   yakın zamanda   tamamlayacağım.\n\nElif";
        var quote = "Bütçe revizyonunu yakın zamanda tamamlayacağım.";

        var result = TurkishQuoteVerifier.VerifyQuote(source, quote);

        Assert.True(result.Found);
        Assert.Equal(
            "Bütçe   revizyonunu   yakın zamanda   tamamlayacağım.",
            source[result.CharStart!.Value..result.CharEnd!.Value]);
    }

    [Fact]
    public void CurlyQuoteAndDashFolding()
    {
        var source = "Toplantı 10–14 Kasım tarihleri arasında; katılımcı sayısı artıyor. O ‘tamam’ dedi.";
        var quote = "O 'tamam' dedi.";

        var result = TurkishQuoteVerifier.VerifyQuote(source, quote);

        Assert.True(result.Found);
    }

    [Fact]
    public void NfcNormalizationOfCombiningCharacters()
    {
        // 'g' + combining breve (decomposed) must normalize the same as precomposed 'ğ'.
        var decomposed = "bu" + "ğ" + "ün"; // bu-g-breve-ün
        var precomposed = "buğün";

        Assert.Equal(
            TurkishTextNormalizer.NormalizeForCompare(decomposed),
            TurkishTextNormalizer.NormalizeForCompare(precomposed));
    }

    [Fact]
    public void OffsetsMapBackIntoOriginalNotNormalizedText()
    {
        var source = "Kayıt:   İYİ    çalışmalar,   teşekkürler.";
        var quote = "iyi çalışmalar";

        var result = TurkishQuoteVerifier.VerifyQuote(source, quote);

        Assert.True(result.Found);
        var matchedOriginal = source[result.CharStart!.Value..result.CharEnd!.Value];
        Assert.Equal(
            TurkishTextNormalizer.NormalizeForCompare(quote),
            TurkishTextNormalizer.NormalizeForCompare(matchedOriginal));
    }

    [Fact]
    public void VerifyEvidenceListUnknownMessageId()
    {
        var evidence = new List<(string MessageId, string Quote)> { ("does-not-exist", "anything") };
        var messages = new Dictionary<string, string> { ["m1"] = "some source text" };

        var results = TurkishQuoteVerifier.VerifyEvidenceList(evidence, messages);

        Assert.Single(results);
        Assert.False(results[0].Found);
        Assert.Equal(QuoteFailureReason.UnknownMessageId, results[0].FailureReason);
    }

    [Fact]
    public void VerifyEvidenceListMixedResults()
    {
        var messages = new Dictionary<string, string> { ["m1"] = "Karar: Proje ertelendi çünkü İzin alınamadı." };
        var evidence = new List<(string MessageId, string Quote)>
        {
            ("m1", "Proje ertelendi çünkü izin alınamadı"),
            ("m1", "bu cümle kaynakta yok"),
        };

        var results = TurkishQuoteVerifier.VerifyEvidenceList(evidence, messages);

        Assert.True(results[0].Found);
        Assert.False(results[1].Found);
    }

    [Theory]
    [InlineData("PROJE ERTELENDİ")]
    [InlineData("proje ertelendi")]
    [InlineData("Proje Ertelendi")]
    public void CaseInsensitiveVariantsAllMatch(string quote)
    {
        var source = "Karar: proje ertelendi, yeni tarih belirlenecek.";

        var result = TurkishQuoteVerifier.VerifyQuote(source, quote);

        Assert.True(result.Found);
    }

    [Fact]
    public void NormalizeWithMapEmptyText()
    {
        var nt = TurkishTextNormalizer.NormalizeWithMap(string.Empty);
        Assert.Equal(string.Empty, nt.Text);
        Assert.Empty(nt.IndexMap);

        var ntNull = TurkishTextNormalizer.NormalizeWithMap(null);
        Assert.Equal(string.Empty, ntNull.Text);
    }
}
