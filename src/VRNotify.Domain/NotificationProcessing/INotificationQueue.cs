namespace VRNotify.Domain.NotificationProcessing;

public interface INotificationQueue
{
    ValueTask EnqueueAsync(NotificationCard card, CancellationToken ct = default);
    IAsyncEnumerable<NotificationCard> DequeueAllAsync(CancellationToken ct = default);
    int Count { get; }
}
