using FluentAssertions;
using VRNotify.Domain.NotificationProcessing;

namespace VRNotify.Domain.Tests.NotificationProcessing;

public class MessageContentTests
{
    [Fact]
    public void TruncatedText_ShortText_ReturnsFullText()
    {
        var content = new MessageContent("Hello", false, MentionType.None);

        content.TruncatedText.Should().Be("Hello");
    }

    [Fact]
    public void TruncatedText_Exactly300Chars_ReturnsFullText()
    {
        var text = new string('a', 300);
        var content = new MessageContent(text, false, MentionType.None);

        content.TruncatedText.Should().Be(text);
        content.TruncatedText.Should().HaveLength(300);
    }

    [Fact]
    public void TruncatedText_301Chars_TruncatesWithEllipsis()
    {
        var text = new string('a', 301);
        var content = new MessageContent(text, false, MentionType.None);

        content.TruncatedText.Should().HaveLength(301); // 300 + "…"
        content.TruncatedText.Should().EndWith("…");
        content.TruncatedText.Should().StartWith(new string('a', 300));
    }

    [Fact]
    public void TruncatedText_LongText_TruncatesAt300()
    {
        var text = new string('x', 1000);
        var content = new MessageContent(text, false, MentionType.None);

        content.TruncatedText.Should().Be(new string('x', 300) + "…");
    }

    [Fact]
    public void MaxDisplayLength_Is300()
    {
        MessageContent.MaxDisplayLength.Should().Be(300);
    }

    [Fact]
    public void RecordEquality_SameValues_AreEqual()
    {
        var a = new MessageContent("Hi", true, MentionType.DirectMention);
        var b = new MessageContent("Hi", true, MentionType.DirectMention);

        a.Should().Be(b);
    }

    [Fact]
    public void RecordEquality_DifferentValues_AreNotEqual()
    {
        var a = new MessageContent("Hi", true, MentionType.DirectMention);
        var b = new MessageContent("Hi", false, MentionType.None);

        a.Should().NotBe(b);
    }
}
