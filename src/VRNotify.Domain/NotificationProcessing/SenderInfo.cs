namespace VRNotify.Domain.NotificationProcessing;

public sealed record SenderInfo(
    string Id,
    string Name,
    string? AvatarUrl);
