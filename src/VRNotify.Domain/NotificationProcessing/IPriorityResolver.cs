namespace VRNotify.Domain.NotificationProcessing;

public interface IPriorityResolver
{
    Priority Resolve(NotificationEvent notification);
}
