using VRNotify.Domain.NotificationProcessing;
using VRNotify.Domain.VRDisplay;

namespace VRNotify.Application.VRDisplay.Services;

public sealed class DisplaySlotManager : IDisplaySlotManager
{
    private readonly List<DisplaySlot> _slots = new();
    private readonly object _lock = new();

    public DisplaySlotManager(int slotCount = 3)
    {
        for (int i = 0; i < slotCount; i++)
            _slots.Add(new DisplaySlot(i));
    }

    public DisplaySlot? FindAvailableSlot()
    {
        lock (_lock)
        {
            return _slots.FirstOrDefault(s => !s.IsOccupied);
        }
    }

    public DisplaySlot? PreemptLowestPriority(Priority incomingPriority)
    {
        throw new NotImplementedException();
    }
}
