using VRNotify.Domain.Common;
using VRNotify.Domain.SourceConnection;

namespace VRNotify.Domain.NotificationProcessing;

public sealed class NotificationCard : Entity
{
    public Guid CardId { get; }
    public Guid OriginEventId { get; }
    public SourceType SourceType { get; }
    public Priority Priority { get; }
    public NotificationState State { get; private set; }
    public string Title { get; }
    public string Body { get; private set; }
    public string SenderDisplay { get; }
    public string? SenderAvatarUrl { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset? DisplayedAt { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }
    public TimeSpan DisplayDuration { get; }

    public NotificationCard(
        Guid cardId,
        Guid originEventId,
        SourceType sourceType,
        Priority priority,
        string title,
        string body,
        string senderDisplay,
        string? senderAvatarUrl,
        TimeSpan displayDuration)
    {
        CardId = cardId;
        OriginEventId = originEventId;
        SourceType = sourceType;
        Priority = priority;
        State = NotificationState.Unread;
        Title = title;
        Body = body;
        SenderDisplay = senderDisplay;
        SenderAvatarUrl = senderAvatarUrl;
        CreatedAt = DateTimeOffset.UtcNow;
        DisplayDuration = displayDuration;
    }

    public void MarkAsDisplayed()
    {
        DisplayedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsRead()
    {
        State = NotificationState.Read;
        ReadAt = DateTimeOffset.UtcNow;
    }

    public void Archive()
    {
        State = NotificationState.Archived;
    }

    public void AppendToBody(string additionalText)
    {
        Body += "\n" + additionalText;
    }
}
