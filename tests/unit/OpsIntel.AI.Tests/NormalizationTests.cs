using OpsIntel.Normalization;
using Xunit;

namespace OpsIntel.AI.Tests;

public sealed class LanguageHintTests
{
    [Fact]
    public void DetectsTurkishFromDiacriticsAndStopwords()
    {
        Assert.Equal(LanguageCode.Tr, LanguageHint.Detect("Merhaba, bu proje için teşekkürler ve iyi çalışmalar."));
    }

    [Fact]
    public void DetectsEnglishFromStopwords()
    {
        Assert.Equal(LanguageCode.En, LanguageHint.Detect("Hello, please review the report and send it to the team."));
    }

    [Fact]
    public void ReturnsUnknownForVeryShortAmbiguousText()
    {
        Assert.Equal(LanguageCode.Unknown, LanguageHint.Detect("OK"));
    }
}

public sealed class ThreadRebuilderTests
{
    [Fact]
    public void GroupsByInReplyToChain()
    {
        var messages = new List<MessageHeader>
        {
            new("m1", null, null, null, "Kickoff", new DateTimeOffset(2026, 8, 3, 9, 0, 0, TimeSpan.Zero)),
            new("m2", null, "m1", ["m1"], "RE: Kickoff", new DateTimeOffset(2026, 8, 3, 10, 0, 0, TimeSpan.Zero)),
            new("m3", null, "m2", ["m1", "m2"], "RE: Kickoff", new DateTimeOffset(2026, 8, 3, 11, 0, 0, TimeSpan.Zero)),
        };

        var threads = ThreadRebuilder.Rebuild(messages);

        var thread = Assert.Single(threads);
        Assert.Equal(["m1", "m2", "m3"], thread.MessageIdsChronological);
    }

    [Fact]
    public void GroupsByConversationIdWhenNoHeaderLinks()
    {
        var messages = new List<MessageHeader>
        {
            new("m1", "conv-1", null, null, "A", new DateTimeOffset(2026, 8, 1, 9, 0, 0, TimeSpan.Zero)),
            new("m2", "conv-1", null, null, "A", new DateTimeOffset(2026, 8, 2, 9, 0, 0, TimeSpan.Zero)),
        };

        var threads = ThreadRebuilder.Rebuild(messages);

        Assert.Single(threads);
    }

    [Fact]
    public void KeepsUnrelatedMessagesInSeparateThreads()
    {
        var messages = new List<MessageHeader>
        {
            new("m1", null, null, null, "Aurora ERP kickoff", new DateTimeOffset(2026, 8, 1, 9, 0, 0, TimeSpan.Zero)),
            new("m2", null, null, null, "Different topic entirely", new DateTimeOffset(2026, 8, 1, 9, 5, 0, TimeSpan.Zero)),
        };

        var threads = ThreadRebuilder.Rebuild(messages);

        Assert.Equal(2, threads.Count);
    }
}

public sealed class TextChunkerTests
{
    [Fact]
    public void OffsetsRoundTripIntoOriginalText()
    {
        var text = "First paragraph here.\n\nSecond paragraph here.\n\nThird paragraph here.";

        var chunks = TextChunker.Chunk(text, targetMaxChars: 1000);

        var single = Assert.Single(chunks);
        Assert.Equal(text, single.Text);
    }

    [Fact]
    public void SplitsIntoMultipleChunksWhenOverTarget()
    {
        var text = string.Join("\n\n", Enumerable.Range(0, 5).Select(i => new string('a', 50) + i));

        var chunks = TextChunker.Chunk(text, targetMaxChars: 60);

        Assert.True(chunks.Count > 1);
        foreach (var chunk in chunks)
        {
            Assert.Equal(chunk.Text, text[chunk.CharStart..chunk.CharEnd]);
        }
    }

    [Fact]
    public void EmptyTextProducesNoChunks()
    {
        Assert.Empty(TextChunker.Chunk(string.Empty));
    }
}
