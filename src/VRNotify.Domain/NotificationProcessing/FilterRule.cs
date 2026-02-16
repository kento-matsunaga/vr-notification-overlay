namespace VRNotify.Domain.NotificationProcessing;

public sealed record FilterRule(
    Guid RuleId,
    FilterRuleType RuleType,
    FilterCondition Condition,
    IReadOnlyDictionary<string, string> Parameters,
    int Order);
