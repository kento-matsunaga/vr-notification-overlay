using VRNotify.Domain.NotificationProcessing;
using VRNotify.Domain.SourceConnection;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;

namespace VRNotify.Infrastructure.Windows;

public sealed class WindowsNotificationAdapter : ISourceAdapter
{
    private UserNotificationListener? _listener;
    private readonly HashSet<uint> _processedIds = new();

    public SourceType SourceType => SourceType.WindowsNotification;
    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
    public event Func<NotificationEvent, Task>? NotificationReceived;
    public event Func<ConnectionState, Task>? ConnectionStateChanged;

    public async Task ConnectAsync(EncryptedCredential credential, CancellationToken ct = default)
    {
        await SetState(ConnectionState.Connecting);

        _listener = UserNotificationListener.Current;
        var access = await _listener.RequestAccessAsync();

        if (access != UserNotificationListenerAccessStatus.Allowed)
        {
            await SetState(ConnectionState.Disconnected);
            throw new InvalidOperationException(
                $"Notification listener access denied: {access}");
        }

        _listener.NotificationChanged += OnNotificationChanged;
        await SetState(ConnectionState.Connected);
    }

    public Task DisconnectAsync(CancellationToken ct = default)
    {
        if (_listener is not null)
        {
            _listener.NotificationChanged -= OnNotificationChanged;
            _listener = null;
        }

        _processedIds.Clear();
        SetState(ConnectionState.Disconnected).GetAwaiter().GetResult();
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        if (_listener is not null)
        {
            _listener.NotificationChanged -= OnNotificationChanged;
            _listener = null;
        }

        _processedIds.Clear();
        return ValueTask.CompletedTask;
    }

    private async void OnNotificationChanged(
        UserNotificationListener sender,
        UserNotificationChangedEventArgs args)
    {
        if (args.ChangeKind != UserNotificationChangedKind.Added)
            return;

        if (!_processedIds.Add(args.UserNotificationId))
            return;

        try
        {
            var notification = sender.GetNotification(args.UserNotificationId);
            if (notification is null)
                return;

            var appName = "Unknown App";
            try
            {
                appName = notification.AppInfo?.DisplayInfo?.DisplayName ?? appName;
            }
            catch
            {
                // Win32 apps may not expose AppInfo
            }

            var (title, body) = ExtractToastText(notification.Notification);

            var evt = new NotificationEvent(
                EventId: Guid.NewGuid(),
                SourceId: Guid.Empty,
                SourceType: SourceType.WindowsNotification,
                Timestamp: DateTimeOffset.Now,
                Sender: new SenderInfo(
                    Id: appName,
                    Name: appName,
                    AvatarUrl: null),
                Channel: new ChannelInfo(
                    Id: appName,
                    Name: title,
                    ServerOrWorkspace: null,
                    IsDirectMessage: false),
                Content: new MessageContent(
                    Text: body,
                    HasAttachment: false,
                    MentionType: MentionType.None));

            if (NotificationReceived is { } handler)
                await handler(evt);
        }
        catch
        {
            // Swallow per-notification errors to keep listener alive
        }
    }

    private static (string Title, string Body) ExtractToastText(Notification notification)
    {
        var binding = notification.Visual?.GetBinding(KnownNotificationBindings.ToastGeneric);
        if (binding is null)
            return ("", "");

        var texts = binding.GetTextElements().ToList();

        var title = texts.Count > 0 ? texts[0].Text ?? "" : "";
        var body = texts.Count > 1
            ? string.Join("\n", texts.Skip(1).Select(t => t.Text ?? ""))
            : "";

        return (title, body);
    }

    private async Task SetState(ConnectionState state)
    {
        State = state;
        if (ConnectionStateChanged is { } handler)
            await handler(state);
    }
}
