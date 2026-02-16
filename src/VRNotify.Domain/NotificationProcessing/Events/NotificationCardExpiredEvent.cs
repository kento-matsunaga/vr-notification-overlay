using VRNotify.Domain.Common;

namespace VRNotify.Domain.NotificationProcessing.Events;

public sealed record NotificationCardExpiredEvent(Guid CardId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
