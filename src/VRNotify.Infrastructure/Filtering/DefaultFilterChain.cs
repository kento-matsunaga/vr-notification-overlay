using VRNotify.Domain.NotificationProcessing;

namespace VRNotify.Infrastructure.Filtering;

public sealed class DefaultFilterChain : IFilterChain
{
    public FilterResult Evaluate(NotificationEvent notification, IReadOnlyList<FilterRule> rules)
    {
        if (rules.Count == 0)
            return new FilterResult(true, null, null);

        foreach (var rule in rules.OrderBy(r => r.Order))
        {
            if (Matches(rule, notification))
            {
                return new FilterResult(
                    rule.Condition == FilterCondition.Include,
                    rule,
                    null);
            }
        }

        // No rule matched: default depends on rule set composition
        // If any Include rules exist -> allowlist mode -> default deny
        // If only Exclude rules    -> blocklist mode -> default allow
        var hasIncludeRules = rules.Any(r => r.Condition == FilterCondition.Include);
        return new FilterResult(!hasIncludeRules, null, null);
    }

    private static bool Matches(FilterRule rule, NotificationEvent notification)
    {
        return rule.RuleType switch
        {
            FilterRuleType.AppName => MatchesAppName(rule, notification),
            _ => false // Only AppName implemented for Booth MVP
        };
    }

    private static bool MatchesAppName(FilterRule rule, NotificationEvent notification)
    {
        if (!rule.Parameters.TryGetValue("AppName", out var appName))
            return false;

        // For Windows notifications, app name is stored in Sender.Name by the adapter
        return string.Equals(notification.Sender.Name, appName, StringComparison.OrdinalIgnoreCase);
    }
}
