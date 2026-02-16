using VRNotify.Domain.NotificationProcessing;
using VRNotify.Domain.SourceConnection;

namespace VRNotify.Application.NotificationProcessing.Services;

public sealed class PriorityResolver
{
    public Priority Resolve(NotificationEvent notification)
    {
        // Booth MVP: all Windows notifications are Low priority
        return notification.SourceType switch
        {
            SourceType.WindowsNotification => Priority.Low,
            _ => Priority.Low
        };
    }
}
