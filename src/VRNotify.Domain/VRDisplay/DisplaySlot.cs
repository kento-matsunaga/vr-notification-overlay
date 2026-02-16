using VRNotify.Domain.NotificationProcessing;

namespace VRNotify.Domain.VRDisplay;

public sealed class DisplaySlot
{
    public int SlotIndex { get; }
    public NotificationCard? CurrentCard { get; private set; }
    public bool IsOccupied => CurrentCard is not null;

    public DisplaySlot(int slotIndex)
    {
        SlotIndex = slotIndex;
    }

    public void Assign(NotificationCard card)
    {
        CurrentCard = card;
    }

    public NotificationCard? Release()
    {
        var card = CurrentCard;
        CurrentCard = null;
        return card;
    }
}
