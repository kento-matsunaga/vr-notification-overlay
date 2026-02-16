using VRNotify.Domain.VRDisplay;

namespace VRNotify.Domain.Configuration;

public sealed record DisplayConfig(
    DisplayPosition Position = DisplayPosition.HmdTop,
    int SlotCount = 3,
    float Opacity = 1.0f,
    float Scale = 1.0f,
    TimeSpan? HighPriorityDuration = null,
    TimeSpan? MediumPriorityDuration = null,
    TimeSpan? LowPriorityDuration = null)
{
    public TimeSpan GetHighPriorityDuration() => HighPriorityDuration ?? TimeSpan.FromSeconds(10);
    public TimeSpan GetMediumPriorityDuration() => MediumPriorityDuration ?? TimeSpan.FromSeconds(7);
    public TimeSpan GetLowPriorityDuration() => LowPriorityDuration ?? TimeSpan.FromSeconds(5);
}
