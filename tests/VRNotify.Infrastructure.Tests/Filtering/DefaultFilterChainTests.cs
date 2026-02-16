using FluentAssertions;
using VRNotify.Domain.NotificationProcessing;
using VRNotify.Domain.SourceConnection;
using VRNotify.Infrastructure.Filtering;

namespace VRNotify.Infrastructure.Tests.Filtering;

public sealed class DefaultFilterChainTests
{
    private readonly DefaultFilterChain _chain = new();

    private static NotificationEvent CreateEvent(string appName = "Discord") => new(
        EventId: Guid.NewGuid(),
        SourceId: Guid.NewGuid(),
        SourceType: SourceType.WindowsNotification,
        Timestamp: DateTimeOffset.UtcNow,
        Sender: new SenderInfo(appName, appName, null),
        Channel: new ChannelInfo(appName, "Toast Title", null, false),
        Content: new MessageContent("Hello!", false, MentionType.None));

    private static FilterRule ExcludeApp(string appName, int order = 1) => new(
        Guid.NewGuid(), FilterRuleType.AppName, FilterCondition.Exclude,
        new Dictionary<string, string> { ["AppName"] = appName }, order);

    private static FilterRule IncludeApp(string appName, int order = 1) => new(
        Guid.NewGuid(), FilterRuleType.AppName, FilterCondition.Include,
        new Dictionary<string, string> { ["AppName"] = appName }, order);

    [Fact]
    public void EmptyRules_AllowsAll()
    {
        var result = _chain.Evaluate(CreateEvent(), Array.Empty<FilterRule>());

        result.IsAllowed.Should().BeTrue();
        result.MatchedRule.Should().BeNull();
    }

    [Fact]
    public void ExcludeRule_BlocksMatchingApp()
    {
        var rules = new[] { ExcludeApp("Discord") };

        var result = _chain.Evaluate(CreateEvent("Discord"), rules);

        result.IsAllowed.Should().BeFalse();
        result.MatchedRule.Should().NotBeNull();
    }

    [Fact]
    public void ExcludeRule_AllowsNonMatchingApp()
    {
        var rules = new[] { ExcludeApp("Slack") };

        var result = _chain.Evaluate(CreateEvent("Discord"), rules);

        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void IncludeRule_AllowsMatchingApp()
    {
        var rules = new[] { IncludeApp("Discord") };

        var result = _chain.Evaluate(CreateEvent("Discord"), rules);

        result.IsAllowed.Should().BeTrue();
        result.MatchedRule.Should().NotBeNull();
    }

    [Fact]
    public void IncludeRule_DeniesNonMatchingApp_AllowlistMode()
    {
        var rules = new[] { IncludeApp("Slack") };

        var result = _chain.Evaluate(CreateEvent("Discord"), rules);

        result.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public void CaseInsensitive_Matching()
    {
        var rules = new[] { ExcludeApp("discord") };

        var result = _chain.Evaluate(CreateEvent("Discord"), rules);

        result.IsAllowed.Should().BeFalse();
    }

    [Fact]
    public void MultipleRules_FirstMatchWins()
    {
        var rules = new[]
        {
            IncludeApp("Discord", order: 1),
            ExcludeApp("Discord", order: 2)
        };

        var result = _chain.Evaluate(CreateEvent("Discord"), rules);

        result.IsAllowed.Should().BeTrue();
        result.MatchedRule!.Condition.Should().Be(FilterCondition.Include);
    }

    [Fact]
    public void RulesOrderedByOrderField()
    {
        var rules = new[]
        {
            ExcludeApp("Discord", order: 2),
            IncludeApp("Discord", order: 1)
        };

        var result = _chain.Evaluate(CreateEvent("Discord"), rules);

        // Include (order=1) should match first despite array position
        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void OnlyExcludeRules_DefaultIsAllow_BlocklistMode()
    {
        var rules = new[]
        {
            ExcludeApp("Slack"),
            ExcludeApp("LINE")
        };

        var result = _chain.Evaluate(CreateEvent("Discord"), rules);

        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void MissingAppNameParameter_DoesNotMatch()
    {
        var ruleWithoutParam = new FilterRule(
            Guid.NewGuid(), FilterRuleType.AppName, FilterCondition.Exclude,
            new Dictionary<string, string>(), 1);

        var result = _chain.Evaluate(CreateEvent("Discord"), new[] { ruleWithoutParam });

        // No match, only Exclude rules -> default allow
        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void NonAppNameRuleType_IsIgnored()
    {
        var serverRule = new FilterRule(
            Guid.NewGuid(), FilterRuleType.Server, FilterCondition.Exclude,
            new Dictionary<string, string> { ["Server"] = "test" }, 1);

        var result = _chain.Evaluate(CreateEvent(), new[] { serverRule });

        // Only Exclude rules, none matched -> default allow
        result.IsAllowed.Should().BeTrue();
    }
}
