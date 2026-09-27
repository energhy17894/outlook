using OpsIntel.Normalization;
using Xunit;

namespace OpsIntel.AI.Tests;

public sealed class EmailBodyCleanerTests
{
    [Fact]
    public void StripsOutlookHeaderReplyBlock_Turkish()
    {
        var body = "Teşekkürler Mehmet, uygun.\n\n" +
                    "Kimden: Mehmet Kaya\n" +
                    "Gönderildi: 3 Ağustos 2026 Pazartesi 09:12\n" +
                    "Kime: Ali Veli\n" +
                    "Konu: Aurora ERP\n\n" +
                    "Aurora ERP Faz 2 için test ortamını Pazartesiye kadar hazır edeceğim.";

        var cleaned = EmailBodyCleaner.Clean(body, isHtml: false);

        Assert.Contains("Teşekkürler Mehmet, uygun.", cleaned.CleanedText);
        Assert.DoesNotContain("Aurora ERP Faz 2 için test ortamını Pazartesiye kadar", cleaned.CleanedText);
        Assert.True(cleaned.RemovedQuotedReply);
    }

    [Fact]
    public void StripsOnWroteReplyBlock_English()
    {
        var body = "Sounds good, thanks!\n\n" +
                    "On 3 Aug 2026, Mehmet Kaya wrote:\n" +
                    "> I will have the test environment ready by Monday.";

        var cleaned = EmailBodyCleaner.Clean(body, isHtml: false);

        Assert.Contains("Sounds good, thanks!", cleaned.CleanedText);
        Assert.DoesNotContain("I will have the test environment ready", cleaned.CleanedText);
        Assert.True(cleaned.RemovedQuotedReply);
    }

    [Fact]
    public void StripsTarihindeYazdiReplyBlock_Turkish()
    {
        var body = "Teşekkürler, uygun.\n\n" +
                    "Ali Veli, 3 Ağustos 2026 tarihinde şunu yazdı:\n" +
                    "> Kullanıcı listesini gönderir misin?";

        var cleaned = EmailBodyCleaner.Clean(body, isHtml: false);

        Assert.Contains("Teşekkürler, uygun.", cleaned.CleanedText);
        Assert.DoesNotContain("Kullanıcı listesini gönderir misin", cleaned.CleanedText);
    }

    [Fact]
    public void StripsOriginalMessageMarker()
    {
        var body = "Onaylıyorum.\n\n-----Original Message-----\nFrom: a@b.com\nSent: today\n\nOld content here.";

        var cleaned = EmailBodyCleaner.Clean(body, isHtml: false);

        Assert.Contains("Onaylıyorum.", cleaned.CleanedText);
        Assert.DoesNotContain("Old content here.", cleaned.CleanedText);
    }

    [Fact]
    public void StripsSignatureBlock_TurkishValediction()
    {
        var body = "Merhaba Ali,\n\nAurora ERP Faz 2 için test ortamını Pazartesiye kadar hazır edeceğim.\n\n" +
                    "İyi çalışmalar,\nMehmet Kaya\nProje Yöneticisi, Örnek A.Ş.\nTel: 0212 555 00 00";

        var cleaned = EmailBodyCleaner.Clean(body, isHtml: false);

        Assert.Contains("Aurora ERP Faz 2 için test ortamını Pazartesiye kadar hazır edeceğim.", cleaned.CleanedText);
        Assert.DoesNotContain("Proje Yöneticisi", cleaned.CleanedText);
        Assert.DoesNotContain("0212 555 00 00", cleaned.CleanedText);
        Assert.True(cleaned.RemovedSignature);
    }

    [Fact]
    public void StripsSignatureBlock_EnglishDashDelimiter()
    {
        var body = "Please review the attached proposal by Friday.\n\n--\nJohn Smith\nAccount Manager\njohn@example.com";

        var cleaned = EmailBodyCleaner.Clean(body, isHtml: false);

        Assert.Contains("Please review the attached proposal by Friday.", cleaned.CleanedText);
        Assert.DoesNotContain("Account Manager", cleaned.CleanedText);
        Assert.True(cleaned.RemovedSignature);
    }

    [Fact]
    public void StripsTurkishConfidentialityDisclaimer()
    {
        var body = "Ekte bulacaksınız.\n\nBu e-posta ve ekleri gizlidir ve yalnızca gönderilen kişiye yöneliktir.";

        var cleaned = EmailBodyCleaner.Clean(body, isHtml: false);

        Assert.Contains("Ekte bulacaksınız.", cleaned.CleanedText);
        Assert.DoesNotContain("gizlidir", cleaned.CleanedText);
        Assert.True(cleaned.RemovedDisclaimer);
    }

    [Fact]
    public void StripsEnglishConfidentialityDisclaimer()
    {
        var body = "Please see attached.\n\nThis email and any attachments is confidential and intended solely for the addressee.";

        var cleaned = EmailBodyCleaner.Clean(body, isHtml: false);

        Assert.Contains("Please see attached.", cleaned.CleanedText);
        Assert.DoesNotContain("confidential", cleaned.CleanedText);
        Assert.True(cleaned.RemovedDisclaimer);
    }

    [Fact]
    public void HtmlToText_ConvertsBlockTagsToNewlinesAndDecodesEntities()
    {
        var html = "<html><body><p>Merhaba &amp; iyi g&uuml;nler</p><div>ikinci satır</div></body></html>";

        var text = EmailBodyCleaner.HtmlToText(html);

        Assert.Contains("Merhaba & iyi günler", text);
        Assert.Contains("ikinci satır", text);
        Assert.DoesNotContain("<p>", text);
    }

    [Fact]
    public void Clean_HandlesNullAndEmptyInputWithoutThrowing()
    {
        var cleanedNull = EmailBodyCleaner.Clean(null, isHtml: false);
        var cleanedEmpty = EmailBodyCleaner.Clean(string.Empty, isHtml: true);

        Assert.Equal(string.Empty, cleanedNull.CleanedText);
        Assert.Equal(string.Empty, cleanedEmpty.CleanedText);
    }

    [Fact]
    public void DoesNotStripShortMessageThatMerelyMentionsFromAsProse()
    {
        // Guards LooksLikeReplyHeaderBlock's false-positive check: a message that opens with
        // "From: Istanbul" as prose (no Sent:/To: within the window) must survive intact.
        var body = "From: Istanbul with love, the whole team says hi.";

        var cleaned = EmailBodyCleaner.Clean(body, isHtml: false);

        Assert.Contains("From: Istanbul with love", cleaned.CleanedText);
        Assert.False(cleaned.RemovedQuotedReply);
    }
}
