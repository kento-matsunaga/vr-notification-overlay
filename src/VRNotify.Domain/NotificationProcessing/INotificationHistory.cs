namespace VRNotify.Domain.NotificationProcessing;

public interface INotificationHistory
{
    Task SaveAsync(NotificationCard card, CancellationToken ct = default);
    Task<IReadOnlyList<NotificationCard>> GetRecentAsync(int count, CancellationToken ct = default);
    Task PurgeOldEntriesAsync(TimeSpan maxAge, int maxCount, CancellationToken ct = default);
}
