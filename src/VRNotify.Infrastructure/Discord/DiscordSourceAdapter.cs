using VRNotify.Domain.NotificationProcessing;
using VRNotify.Domain.SourceConnection;

namespace VRNotify.Infrastructure.Discord;

public sealed class DiscordSourceAdapter : ISourceAdapter
{
    public SourceType SourceType => SourceType.Discord;
    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
    public event Func<NotificationEvent, Task>? NotificationReceived;
    public event Func<ConnectionState, Task>? ConnectionStateChanged;

    public Task ConnectAsync(EncryptedCredential credential, CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task DisconnectAsync(CancellationToken ct = default)
        => throw new NotImplementedException();

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
