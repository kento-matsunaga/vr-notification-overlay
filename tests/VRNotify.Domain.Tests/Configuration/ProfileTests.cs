using FluentAssertions;
using VRNotify.Domain.Configuration;
using VRNotify.Domain.NotificationProcessing;

namespace VRNotify.Domain.Tests.Configuration;

public class ProfileTests
{
    private static Profile CreateProfile(string name = "Test", bool isDefault = false) =>
        new(Guid.NewGuid(), name, isDefault);

    [Fact]
    public void Constructor_SetsProperties()
    {
        var id = Guid.NewGuid();
        var profile = new Profile(id, "Gaming", true);

        profile.ProfileId.Should().Be(id);
        profile.Name.Should().Be("Gaming");
        profile.IsDefault.Should().BeTrue();
    }

    [Fact]
    public void Constructor_InitializesDefaultDisplayConfig()
    {
        var profile = CreateProfile();

        profile.Display.Should().NotBeNull();
        profile.Display.SlotCount.Should().Be(3);
    }

    [Fact]
    public void Constructor_InitializesDefaultDndSettings()
    {
        var profile = CreateProfile();

        profile.Dnd.Should().NotBeNull();
        profile.Dnd.Mode.Should().Be(DndMode.Off);
    }

    [Fact]
    public void Constructor_FilterRulesEmpty()
    {
        var profile = CreateProfile();

        profile.FilterRules.Should().BeEmpty();
    }

    [Fact]
    public void Constructor_EnabledSourceIdsEmpty()
    {
        var profile = CreateProfile();

        profile.EnabledSourceIds.Should().BeEmpty();
    }

    [Fact]
    public void UpdateName_ChangesName()
    {
        var profile = CreateProfile(name: "Old");

        profile.UpdateName("New");

        profile.Name.Should().Be("New");
    }

    [Fact]
    public void UpdateDisplay_ReplacesConfig()
    {
        var profile = CreateProfile();
        var newConfig = new DisplayConfig(SlotCount: 5, Opacity: 0.8f);

        profile.UpdateDisplay(newConfig);

        profile.Display.SlotCount.Should().Be(5);
        profile.Display.Opacity.Should().Be(0.8f);
    }

    [Fact]
    public void UpdateDnd_ReplacesSettings()
    {
        var profile = CreateProfile();
        var newDnd = new DndSettings(DndMode.HighPriorityOnly);

        profile.UpdateDnd(newDnd);

        profile.Dnd.Mode.Should().Be(DndMode.HighPriorityOnly);
    }

    [Fact]
    public void AddFilterRule_AddsToCollection()
    {
        var profile = CreateProfile();
        var rule = new FilterRule(
            Guid.NewGuid(), FilterRuleType.Channel, FilterCondition.Include,
            new Dictionary<string, string> { ["channelId"] = "123" }, 1);

        profile.AddFilterRule(rule);

        profile.FilterRules.Should().ContainSingle().Which.Should().Be(rule);
    }

    [Fact]
    public void RemoveFilterRule_RemovesFromCollection()
    {
        var profile = CreateProfile();
        var ruleId = Guid.NewGuid();
        var rule = new FilterRule(
            ruleId, FilterRuleType.Keyword, FilterCondition.Exclude,
            new Dictionary<string, string> { ["keyword"] = "spam" }, 0);
        profile.AddFilterRule(rule);

        profile.RemoveFilterRule(ruleId);

        profile.FilterRules.Should().BeEmpty();
    }

    [Fact]
    public void RemoveFilterRule_NonexistentId_DoesNotThrow()
    {
        var profile = CreateProfile();

        var act = () => profile.RemoveFilterRule(Guid.NewGuid());

        act.Should().NotThrow();
    }

    [Fact]
    public void EnableSource_AddsSourceId()
    {
        var profile = CreateProfile();
        var sourceId = Guid.NewGuid();

        profile.EnableSource(sourceId);

        profile.EnabledSourceIds.Should().ContainSingle().Which.Should().Be(sourceId);
    }

    [Fact]
    public void EnableSource_Duplicate_DoesNotAddTwice()
    {
        var profile = CreateProfile();
        var sourceId = Guid.NewGuid();

        profile.EnableSource(sourceId);
        profile.EnableSource(sourceId);

        profile.EnabledSourceIds.Should().HaveCount(1);
    }

    [Fact]
    public void DisableSource_RemovesSourceId()
    {
        var profile = CreateProfile();
        var sourceId = Guid.NewGuid();
        profile.EnableSource(sourceId);

        profile.DisableSource(sourceId);

        profile.EnabledSourceIds.Should().BeEmpty();
    }

    [Fact]
    public void DisableSource_NonexistentId_DoesNotThrow()
    {
        var profile = CreateProfile();

        var act = () => profile.DisableSource(Guid.NewGuid());

        act.Should().NotThrow();
    }
}
