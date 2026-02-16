using VRNotify.Domain.NotificationProcessing;
using VRNotify.Domain.VRDisplay;

namespace VRNotify.Overlay.OpenVR;

public sealed class OpenVrOverlayManager : IOverlayManager
{
    public bool IsAvailable { get; private set; }

    public Task InitializeAsync(CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task ShowNotificationAsync(NotificationCard card, DisplaySlot slot, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task HideSlotAsync(DisplaySlot slot, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task UpdatePositionAsync(DisplayPosition position, CancellationToken ct = default)
        => throw new NotImplementedException();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
