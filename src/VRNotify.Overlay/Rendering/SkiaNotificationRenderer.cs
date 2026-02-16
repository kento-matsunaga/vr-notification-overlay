using VRNotify.Domain.NotificationProcessing;
using VRNotify.Domain.VRDisplay;

namespace VRNotify.Overlay.Rendering;

public sealed class SkiaNotificationRenderer : IOverlayRenderer
{
    public byte[] RenderCard(NotificationCard card, int width, int height)
        => throw new NotImplementedException();
}
