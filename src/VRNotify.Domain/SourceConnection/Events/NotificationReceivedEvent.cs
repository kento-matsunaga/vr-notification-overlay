using VRNotify.Domain.Common;
using VRNotify.Domain.NotificationProcessing;

namespace VRNotify.Domain.SourceConnection.Events;

public sealed record NotificationReceivedEvent(NotificationEvent Notification) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
