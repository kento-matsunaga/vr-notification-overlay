using FluentAssertions;
using VRNotify.Domain.Configuration;
using VRNotify.Domain.NotificationProcessing;
using VRNotify.Domain.VRDisplay;
using VRNotify.Infrastructure.Persistence;

namespace VRNotify.Infrastructure.Tests.Persistence;

public sealed class JsonSettingsRepositoryTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _filePath;

    public JsonSettingsRepositoryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "VRNotifyTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _filePath = Path.Combine(_tempDir, "settings.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task LoadAsync_FileNotFound_ReturnsDefaults()
    {
        var repo = new JsonSettingsRepository(_filePath);

        var settings = await repo.LoadAsync();

        settings.Should().NotBeNull();
        settings.Profiles.Should().HaveCount(1);
        settings.GetActiveProfile().IsDefault.Should().BeTrue();
        settings.Audio.IsEnabled.Should().BeTrue();
        settings.History.RetentionDays.Should().Be(7);
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrips_DefaultSettings()
    {
        var repo = new JsonSettingsRepository(_filePath);
        var original = new UserSettings();

        await repo.SaveAsync(original);
        var loaded = await repo.LoadAsync();

        loaded.SchemaVersion.Should().Be(original.SchemaVersion);
        loaded.ActiveProfileId.Should().Be(original.ActiveProfileId);
        loaded.Audio.Should().Be(original.Audio);
        loaded.History.Should().Be(original.History);
        loaded.Profiles.Should().HaveCount(1);
        loaded.GetActiveProfile().Name.Should().Be("Default");
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrips_CustomSettings()
    {
        var repo = new JsonSettingsRepository(_filePath);
        var settings = new UserSettings();
        settings.UpdateAudio(new AudioConfig(IsEnabled: false, Volume: 0.8f));
        settings.UpdateHistory(new HistoryConfig(RetentionDays: 14, MaxEntries: 500));

        var profile = settings.GetActiveProfile();
        profile.UpdateDisplay(new DisplayConfig(
            Position: DisplayPosition.HmdBottom,
            SlotCount: 5,
            Opacity: 0.7f,
            Scale: 1.5f));
        profile.UpdateDnd(new DndSettings(DndMode.HighPriorityOnly));
        profile.AddFilterRule(new FilterRule(
            Guid.NewGuid(),
            FilterRuleType.AppName,
            FilterCondition.Exclude,
            new Dictionary<string, string> { ["AppName"] = "Discord" },
            Order: 1));

        await repo.SaveAsync(settings);
        var loaded = await repo.LoadAsync();

        loaded.Audio.IsEnabled.Should().BeFalse();
        loaded.Audio.Volume.Should().Be(0.8f);
        loaded.History.RetentionDays.Should().Be(14);
        loaded.History.MaxEntries.Should().Be(500);

        var loadedProfile = loaded.GetActiveProfile();
        loadedProfile.Display.Position.Should().Be(DisplayPosition.HmdBottom);
        loadedProfile.Display.SlotCount.Should().Be(5);
        loadedProfile.Display.Opacity.Should().Be(0.7f);
        loadedProfile.Display.Scale.Should().Be(1.5f);
        loadedProfile.Dnd.Mode.Should().Be(DndMode.HighPriorityOnly);
        loadedProfile.FilterRules.Should().HaveCount(1);
        loadedProfile.FilterRules[0].RuleType.Should().Be(FilterRuleType.AppName);
        loadedProfile.FilterRules[0].Parameters["AppName"].Should().Be("Discord");
    }

    [Fact]
    public async Task SaveAsync_CreatesDirectory_WhenNotExists()
    {
        var nestedPath = Path.Combine(_tempDir, "nested", "deep", "settings.json");
        var repo = new JsonSettingsRepository(nestedPath);

        await repo.SaveAsync(new UserSettings());

        File.Exists(nestedPath).Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_CorruptedJson_ReturnsDefaults()
    {
        await File.WriteAllTextAsync(_filePath, "{ invalid json !!!}}}");
        var repo = new JsonSettingsRepository(_filePath);

        var settings = await repo.LoadAsync();

        settings.Should().NotBeNull();
        settings.Profiles.Should().HaveCount(1);
    }

    [Fact]
    public async Task SaveAndLoad_PreservesProfileId()
    {
        var repo = new JsonSettingsRepository(_filePath);
        var original = new UserSettings();
        var expectedProfileId = original.ActiveProfileId;

        await repo.SaveAsync(original);
        var loaded = await repo.LoadAsync();

        loaded.ActiveProfileId.Should().Be(expectedProfileId);
        loaded.GetActiveProfile().ProfileId.Should().Be(expectedProfileId);
    }

    [Fact]
    public async Task SaveAndLoad_PreservesMultipleFilterRules()
    {
        var repo = new JsonSettingsRepository(_filePath);
        var settings = new UserSettings();
        var profile = settings.GetActiveProfile();

        profile.AddFilterRule(new FilterRule(
            Guid.NewGuid(), FilterRuleType.AppName, FilterCondition.Include,
            new Dictionary<string, string> { ["AppName"] = "Discord" }, 1));
        profile.AddFilterRule(new FilterRule(
            Guid.NewGuid(), FilterRuleType.AppName, FilterCondition.Include,
            new Dictionary<string, string> { ["AppName"] = "Slack" }, 2));
        profile.AddFilterRule(new FilterRule(
            Guid.NewGuid(), FilterRuleType.AppName, FilterCondition.Exclude,
            new Dictionary<string, string> { ["AppName"] = "SystemApp" }, 3));

        await repo.SaveAsync(settings);
        var loaded = await repo.LoadAsync();

        loaded.GetActiveProfile().FilterRules.Should().HaveCount(3);
    }
}
