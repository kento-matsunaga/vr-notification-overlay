using CommunityToolkit.Mvvm.ComponentModel;
using VRNotify.Domain.Configuration;
using VRNotify.Domain.VRDisplay;

namespace VRNotify.Desktop.ViewModels;

public sealed partial class DisplayViewModel : ObservableObject
{
    [ObservableProperty]
    private DisplayPosition _position = DisplayPosition.HmdTop;

    [ObservableProperty]
    private int _displayDurationSeconds = 5;

    [ObservableProperty]
    private double _opacity = 1.0;

    [ObservableProperty]
    private double _scale = 1.0;

    [ObservableProperty]
    private int _slotCount = 3;

    public DisplayPosition[] AvailablePositions { get; } = Enum.GetValues<DisplayPosition>();

    public void LoadFromProfile(Profile profile)
    {
        Position = profile.Display.Position;
        DisplayDurationSeconds = (int)profile.Display.GetLowPriorityDuration().TotalSeconds;
        Opacity = profile.Display.Opacity;
        Scale = profile.Display.Scale;
        SlotCount = profile.Display.SlotCount;
    }

    public void ApplyToProfile(Profile profile)
    {
        var duration = TimeSpan.FromSeconds(DisplayDurationSeconds);
        profile.UpdateDisplay(new DisplayConfig(
            Position: Position,
            SlotCount: SlotCount,
            Opacity: (float)Opacity,
            Scale: (float)Scale,
            LowPriorityDuration: duration,
            MediumPriorityDuration: duration + TimeSpan.FromSeconds(2),
            HighPriorityDuration: duration + TimeSpan.FromSeconds(5)));
    }
}
