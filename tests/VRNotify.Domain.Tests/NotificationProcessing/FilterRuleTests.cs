using FluentAssertions;
using VRNotify.Domain.NotificationProcessing;

namespace VRNotify.Domain.Tests.NotificationProcessing;

public class FilterRuleTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var ruleId = Guid.NewGuid();
        var parameters = new Dictionary<string, string> { ["channelId"] = "123" };

        var rule = new FilterRule(ruleId, FilterRuleType.Channel, FilterCondition.Include, parameters, 1);

        rule.RuleId.Should().Be(ruleId);
        rule.RuleType.Should().Be(FilterRuleType.Channel);
        rule.Condition.Should().Be(FilterCondition.Include);
        rule.Parameters.Should().ContainKey("channelId").WhoseValue.Should().Be("123");
        rule.Order.Should().Be(1);
    }

    [Fact]
    public void RecordEquality_SameValues_AreEqual()
    {
        var id = Guid.NewGuid();
        var parameters = new Dictionary<string, string> { ["key"] = "value" };
        var a = new FilterRule(id, FilterRuleType.Keyword, FilterCondition.Exclude, parameters, 0);
        var b = new FilterRule(id, FilterRuleType.Keyword, FilterCondition.Exclude, parameters, 0);

        a.Should().Be(b);
    }

    [Fact]
    public void Parameters_IsReadOnlyDictionary()
    {
        var parameters = new Dictionary<string, string> { ["k"] = "v" };
        var rule = new FilterRule(Guid.NewGuid(), FilterRuleType.Server, FilterCondition.Include, parameters, 0);

        rule.Parameters.Should().BeAssignableTo<IReadOnlyDictionary<string, string>>();
    }
}
