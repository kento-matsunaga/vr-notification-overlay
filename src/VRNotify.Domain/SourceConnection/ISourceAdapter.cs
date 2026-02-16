using VRNotify.Domain.NotificationProcessing;

namespace VRNotify.Domain.SourceConnection;

public interface ISourceAdapter : IAsyncDisposable
{
    SourceType SourceType { get; }
    ConnectionState State { get; }
    event Func<NotificationEvent, Task>? NotificationReceived;
    event Func<ConnectionState, Task>? ConnectionStateChanged;
    Task ConnectAsync(EncryptedCredential credential, CancellationToken ct = default);
    Task DisconnectAsync(CancellationToken ct = default);
}
