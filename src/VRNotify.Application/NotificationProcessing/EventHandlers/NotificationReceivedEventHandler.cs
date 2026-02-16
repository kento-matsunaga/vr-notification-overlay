using MediatR;
using VRNotify.Application.Common;
using VRNotify.Domain.SourceConnection.Events;

namespace VRNotify.Application.NotificationProcessing.EventHandlers;

public sealed class NotificationReceivedEventHandler
    : INotificationHandler<DomainEventNotification<NotificationReceivedEvent>>
{
    public Task Handle(DomainEventNotification<NotificationReceivedEvent> notification, CancellationToken cancellationToken)
    {
        // TODO: Filter → Priority → Bundle → Create Card → Save History → Enqueue for display
        throw new NotImplementedException();
    }
}
