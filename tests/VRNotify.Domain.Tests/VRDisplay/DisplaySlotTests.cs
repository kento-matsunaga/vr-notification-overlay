using FluentAssertions;
using VRNotify.Domain.NotificationProcessing;
using VRNotify.Domain.SourceConnection;
using VRNotify.Domain.VRDisplay;

namespace VRNotify.Domain.Tests.VRDisplay;

public class DisplaySlotTests
{
    private static NotificationCard CreateCard() =>
        new(
            cardId: Guid.NewGuid(),
            originEventId: Guid.NewGuid(),
            sourceType: SourceType.Discord,
            priority: Priority.Medium,
            title: "#general",
            body: "Test message",
            senderDisplay: "TestUser",
            senderAvatarUrl: null,
            displayDuration: TimeSpan.FromSeconds(7));

    [Fact]
    public void Constructor_SetsSlotIndex()
    {
        var slot = new DisplaySlot(2);

        slot.SlotIndex.Should().Be(2);
    }

    [Fact]
    public void Constructor_IsNotOccupied()
    {
        var slot = new DisplaySlot(0);

        slot.IsOccupied.Should().BeFalse();
        slot.CurrentCard.Should().BeNull();
    }

    [Fact]
    public void Assign_SetsCurrentCard()
    {
        var slot = new DisplaySlot(0);
        var card = CreateCard();

        slot.Assign(card);

        slot.CurrentCard.Should().Be(card);
    }

    [Fact]
    public void Assign_MakesSlotOccupied()
    {
        var slot = new DisplaySlot(0);
        var card = CreateCard();

        slot.Assign(card);

        slot.IsOccupied.Should().BeTrue();
    }

    [Fact]
    public void Release_ReturnsCurrentCard()
    {
        var slot = new DisplaySlot(0);
        var card = CreateCard();
        slot.Assign(card);

        var released = slot.Release();

        released.Should().Be(card);
    }

    [Fact]
    public void Release_ClearsSlot()
    {
        var slot = new DisplaySlot(0);
        slot.Assign(CreateCard());

        slot.Release();

        slot.IsOccupied.Should().BeFalse();
        slot.CurrentCard.Should().BeNull();
    }

    [Fact]
    public void Release_EmptySlot_ReturnsNull()
    {
        var slot = new DisplaySlot(0);

        var released = slot.Release();

        released.Should().BeNull();
    }
}
