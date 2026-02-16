using VRNotify.Domain.NotificationProcessing;

namespace VRNotify.Domain.VRDisplay;

public interface IOverlayRenderer
{
    byte[] RenderCard(NotificationCard card, int width, int height);
}
