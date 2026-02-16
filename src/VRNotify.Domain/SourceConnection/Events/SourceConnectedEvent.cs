using VRNotify.Domain.Common;

namespace VRNotify.Domain.SourceConnection.Events;

public sealed record SourceConnectedEvent(Guid SourceId, SourceType SourceType) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
