using VRNotify.Domain.Common;

namespace VRNotify.Domain.SourceConnection;

public sealed class NotificationSource : Entity
{
    public Guid SourceId { get; }
    public SourceType SourceType { get; }
    public string DisplayName { get; private set; }
    public ConnectionState ConnectionState { get; private set; }
    public bool IsEnabled { get; private set; }

    public NotificationSource(Guid sourceId, SourceType sourceType, string displayName)
    {
        SourceId = sourceId;
        SourceType = sourceType;
        DisplayName = displayName;
        ConnectionState = ConnectionState.Disconnected;
        IsEnabled = true;
    }

    public void UpdateDisplayName(string name) => DisplayName = name;
    public void Enable() => IsEnabled = true;
    public void Disable() => IsEnabled = false;

    public void TransitionTo(ConnectionState newState)
    {
        ConnectionState = newState;
    }
}
