namespace VRNotify.Domain.VRDisplay;

public interface IDisplaySlotManager
{
    DisplaySlot? FindAvailableSlot();
}
