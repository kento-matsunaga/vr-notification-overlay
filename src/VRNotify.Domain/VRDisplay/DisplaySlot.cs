using VRNotify.Domain.NotificationProcessing;

namespace VRNotify.Domain.VRDisplay;

public sealed class DisplaySlot
{
    private readonly object _lock = new();

    public int SlotIndex { get; }
    public NotificationCard? CurrentCard { get; private set; }
    public bool IsOccupied => CurrentCard is not null;

    public DisplaySlot(int slotIndex)
    {
        SlotIndex = slotIndex;
    }

    public void Assign(NotificationCard card)
    {
        lock (_lock)
        {
            CurrentCard = card;
        }
    }

    public NotificationCard? Release()
    {
        lock (_lock)
        {
            var card = CurrentCard;
            CurrentCard = null;
            return card;
        }
    }
}
