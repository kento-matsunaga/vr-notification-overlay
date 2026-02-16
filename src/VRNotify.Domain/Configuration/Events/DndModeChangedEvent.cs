using VRNotify.Domain.Common;

namespace VRNotify.Domain.Configuration.Events;

public sealed record DndModeChangedEvent(DndMode NewMode) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
