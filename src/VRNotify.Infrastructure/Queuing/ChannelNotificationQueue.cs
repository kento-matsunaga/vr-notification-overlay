using System.Threading.Channels;
using VRNotify.Domain.NotificationProcessing;

namespace VRNotify.Infrastructure.Queuing;

public sealed class ChannelNotificationQueue : INotificationQueue
{
    private readonly Channel<NotificationCard> _channel =
        Channel.CreateBounded<NotificationCard>(100);

    public int Count => _channel.Reader.Count;

    public async ValueTask EnqueueAsync(NotificationCard card, CancellationToken ct = default)
        => await _channel.Writer.WriteAsync(card, ct);

    public async IAsyncEnumerable<NotificationCard> DequeueAllAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var card in _channel.Reader.ReadAllAsync(ct))
            yield return card;
    }
}
