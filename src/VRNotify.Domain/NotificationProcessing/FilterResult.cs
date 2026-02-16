namespace VRNotify.Domain.NotificationProcessing;

public sealed record FilterResult(
    bool IsAllowed,
    FilterRule? MatchedRule,
    Priority? OverridePriority);
