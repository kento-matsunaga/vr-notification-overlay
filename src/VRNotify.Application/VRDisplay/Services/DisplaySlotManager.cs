using VRNotify.Domain.NotificationProcessing;
using VRNotify.Domain.VRDisplay;

namespace VRNotify.Application.VRDisplay.Services;

public sealed class DisplaySlotManager
{
    private readonly List<DisplaySlot> _slots = new();

    public DisplaySlotManager(int slotCount = 3)
    {
        for (int i = 0; i < slotCount; i++)
            _slots.Add(new DisplaySlot(i));
    }

    public DisplaySlot? FindAvailableSlot()
    {
        return _slots.FirstOrDefault(s => !s.IsOccupied);
    }

    public DisplaySlot? PreemptLowestPriority(Priority incomingPriority)
    {
        throw new NotImplementedException();
    }
}
