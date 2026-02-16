using FluentAssertions;
using VRNotify.Domain.NotificationProcessing;
using VRNotify.Domain.SourceConnection;

namespace VRNotify.Domain.Tests.NotificationProcessing;

public class NotificationCardTests
{
    private static NotificationCard CreateCard(
        Priority priority = Priority.Medium,
        string body = "Test body") =>
        new(
            cardId: Guid.NewGuid(),
            originEventId: Guid.NewGuid(),
            sourceType: SourceType.Discord,
            priority: priority,
            title: "#general",
            body: body,
            senderDisplay: "TestUser",
            senderAvatarUrl: "https://example.com/avatar.png",
            displayDuration: TimeSpan.FromSeconds(7));

    [Fact]
    public void Constructor_InitializesStateToUnread()
    {
        var card = CreateCard();

        card.State.Should().Be(NotificationState.Unread);
    }

    [Fact]
    public void Constructor_SetsCreatedAtToNow()
    {
        var before = DateTimeOffset.UtcNow;
        var card = CreateCard();
        var after = DateTimeOffset.UtcNow;

        card.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var cardId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var card = new NotificationCard(
            cardId, eventId, SourceType.Slack, Priority.High,
            "title", "body", "sender", null, TimeSpan.FromSeconds(10));

        card.CardId.Should().Be(cardId);
        card.OriginEventId.Should().Be(eventId);
        card.SourceType.Should().Be(SourceType.Slack);
        card.Priority.Should().Be(Priority.High);
        card.Title.Should().Be("title");
        card.Body.Should().Be("body");
        card.SenderDisplay.Should().Be("sender");
        card.SenderAvatarUrl.Should().BeNull();
        card.DisplayDuration.Should().Be(TimeSpan.FromSeconds(10));
        card.DisplayedAt.Should().BeNull();
        card.ReadAt.Should().BeNull();
    }

    [Fact]
    public void MarkAsDisplayed_SetsDisplayedAtTimestamp()
    {
        var card = CreateCard();

        var before = DateTimeOffset.UtcNow;
        card.MarkAsDisplayed();
        var after = DateTimeOffset.UtcNow;

        card.DisplayedAt.Should().NotBeNull();
        card.DisplayedAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void MarkAsDisplayed_DoesNotChangeState()
    {
        var card = CreateCard();

        card.MarkAsDisplayed();

        card.State.Should().Be(NotificationState.Unread);
    }

    [Fact]
    public void MarkAsRead_TransitionsStateToRead()
    {
        var card = CreateCard();

        card.MarkAsRead();

        card.State.Should().Be(NotificationState.Read);
    }

    [Fact]
    public void MarkAsRead_SetsReadAtTimestamp()
    {
        var card = CreateCard();

        var before = DateTimeOffset.UtcNow;
        card.MarkAsRead();
        var after = DateTimeOffset.UtcNow;

        card.ReadAt.Should().NotBeNull();
        card.ReadAt!.Value.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Archive_TransitionsStateToArchived()
    {
        var card = CreateCard();

        card.Archive();

        card.State.Should().Be(NotificationState.Archived);
    }

    [Fact]
    public void AppendToBody_AppendsWithNewline()
    {
        var card = CreateCard(body: "Hello");

        card.AppendToBody("World");

        card.Body.Should().Be("Hello\nWorld");
    }

    [Fact]
    public void AppendToBody_MultipleAppends_AccumulatesText()
    {
        var card = CreateCard(body: "Line1");

        card.AppendToBody("Line2");
        card.AppendToBody("Line3");

        card.Body.Should().Be("Line1\nLine2\nLine3");
    }

    [Fact]
    public void DomainEvents_InitiallyEmpty()
    {
        var card = CreateCard();

        card.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void ClearDomainEvents_ClearsAllEvents()
    {
        var card = CreateCard();
        // Entity base class exposes ClearDomainEvents; no events raised by card itself
        card.ClearDomainEvents();

        card.DomainEvents.Should().BeEmpty();
    }
}
