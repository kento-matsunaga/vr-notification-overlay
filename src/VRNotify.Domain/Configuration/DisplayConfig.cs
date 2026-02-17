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
    /// <summary>Offset added to base duration for Medium priority notifications.</summary>
    public static readonly TimeSpan MediumPriorityOffset = TimeSpan.FromSeconds(2);

    /// <summary>Offset added to base duration for High priority notifications.</summary>
    public static readonly TimeSpan HighPriorityOffset = TimeSpan.FromSeconds(5);

    public TimeSpan GetHighPriorityDuration() => HighPriorityDuration ?? TimeSpan.FromSeconds(10);
    public TimeSpan GetMediumPriorityDuration() => MediumPriorityDuration ?? TimeSpan.FromSeconds(7);
    public TimeSpan GetLowPriorityDuration() => LowPriorityDuration ?? TimeSpan.FromSeconds(5);

    /// <summary>
    /// Creates a DisplayConfig by computing Medium/High durations from a base (Low) duration
    /// using the standard priority offset rules.
    /// </summary>
    public static DisplayConfig WithBaseDuration(
        TimeSpan baseDuration,
        DisplayPosition position = DisplayPosition.HmdTop,
        int slotCount = 3,
        float opacity = 1.0f,
        float scale = 1.0f)
    {
        return new DisplayConfig(
            Position: position,
            SlotCount: slotCount,
            Opacity: opacity,
            Scale: scale,
            LowPriorityDuration: baseDuration,
            MediumPriorityDuration: baseDuration + MediumPriorityOffset,
            HighPriorityDuration: baseDuration + HighPriorityOffset);
    }
}
