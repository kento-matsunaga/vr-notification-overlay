namespace VRNotify.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
