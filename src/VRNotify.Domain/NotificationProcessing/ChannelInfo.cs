using VRNotify.Domain.SourceConnection;

namespace VRNotify.Domain.NotificationProcessing;

public sealed record ChannelInfo(
    string Id,
    string Name,
    string? ServerOrWorkspace,
    bool IsDirectMessage);
