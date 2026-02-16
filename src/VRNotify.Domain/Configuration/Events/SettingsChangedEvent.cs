using VRNotify.Domain.Common;

namespace VRNotify.Domain.Configuration.Events;

public sealed record SettingsChangedEvent(string SettingName) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
