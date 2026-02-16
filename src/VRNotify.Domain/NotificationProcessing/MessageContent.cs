namespace VRNotify.Domain.NotificationProcessing;

public sealed record MessageContent(
    string Text,
    bool HasAttachment,
    MentionType MentionType)
{
    public const int MaxDisplayLength = 300;

    public string TruncatedText => Text.Length > MaxDisplayLength
        ? Text[..MaxDisplayLength] + "…"
        : Text;
}
