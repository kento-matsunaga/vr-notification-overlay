using VRNotify.Domain.NotificationProcessing;

namespace VRNotify.Domain.VRDisplay;

public interface IOverlayManager : IAsyncDisposable
{
    bool IsAvailable { get; }
    Task InitializeAsync(CancellationToken ct = default);
    Task ShowNotificationAsync(NotificationCard card, DisplaySlot slot, CancellationToken ct = default);
    Task HideSlotAsync(DisplaySlot slot, CancellationToken ct = default);
    Task UpdatePositionAsync(DisplayPosition position, CancellationToken ct = default);
}
