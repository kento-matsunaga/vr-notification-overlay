using MediatR;
using VRNotify.Domain.Common;

namespace VRNotify.Application.Common;

public sealed class DomainEventNotification<TEvent> : INotification where TEvent : IDomainEvent
{
    public TEvent DomainEvent { get; }

    public DomainEventNotification(TEvent domainEvent)
    {
        DomainEvent = domainEvent;
    }
}
