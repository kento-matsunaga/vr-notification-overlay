using VRNotify.Domain.NotificationProcessing;

namespace VRNotify.Infrastructure.Persistence;

public sealed class SqliteNotificationHistory : INotificationHistory
{
    public Task SaveAsync(NotificationCard card, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task<IReadOnlyList<NotificationCard>> GetRecentAsync(int count, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task PurgeOldEntriesAsync(TimeSpan maxAge, int maxCount, CancellationToken ct = default)
        => throw new NotImplementedException();
}
