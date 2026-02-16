using VRNotify.Domain.Common;

namespace VRNotify.Domain.NotificationProcessing.Events;

public sealed record NotificationCardCreatedEvent(NotificationCard Card) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
