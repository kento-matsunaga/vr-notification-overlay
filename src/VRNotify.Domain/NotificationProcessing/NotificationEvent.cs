using System.Text.Json;
using VRNotify.Domain.SourceConnection;

namespace VRNotify.Domain.NotificationProcessing;

public sealed record NotificationEvent(
    Guid EventId,
    Guid SourceId,
    SourceType SourceType,
    DateTimeOffset Timestamp,
    SenderInfo Sender,
    ChannelInfo Channel,
    MessageContent Content);
