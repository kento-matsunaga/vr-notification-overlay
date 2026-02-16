using VRNotify.Domain.Common;

namespace VRNotify.Domain.SourceConnection.Events;

public sealed record SourceDisconnectedEvent(Guid SourceId, SourceType SourceType, string? Reason) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
