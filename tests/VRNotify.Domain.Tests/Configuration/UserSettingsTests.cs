using FluentAssertions;
using VRNotify.Domain.Configuration;

namespace VRNotify.Domain.Tests.Configuration;

public class UserSettingsTests
{
    [Fact]
    public void Constructor_CreatesDefaultProfile()
    {
        var settings = new UserSettings();

        settings.Profiles.Should().ContainSingle();
        settings.Profiles[0].IsDefault.Should().BeTrue();
        settings.Profiles[0].Name.Should().Be("Default");
    }

    [Fact]
    public void Constructor_SetsActiveProfileToDefault()
    {
        var settings = new UserSettings();

        settings.ActiveProfileId.Should().Be(settings.Profiles[0].ProfileId);
    }

    [Fact]
    public void Constructor_InitializesAudioConfig()
    {
        var settings = new UserSettings();

        settings.Audio.Should().NotBeNull();
        settings.Audio.IsEnabled.Should().BeTrue();
        settings.Audio.Volume.Should().Be(0.3f);
    }

    [Fact]
    public void Constructor_InitializesHistoryConfig()
    {
        var settings = new UserSettings();

        settings.History.Should().NotBeNull();
        settings.History.RetentionDays.Should().Be(7);
        settings.History.MaxEntries.Should().Be(1000);
    }

    [Fact]
    public void SchemaVersion_Is1Point0()
    {
        var settings = new UserSettings();

        settings.SchemaVersion.Should().Be("1.0");
    }

    [Fact]
    public void GetActiveProfile_ReturnsDefaultProfile()
    {
        var settings = new UserSettings();

        var active = settings.GetActiveProfile();

        active.IsDefault.Should().BeTrue();
        active.Name.Should().Be("Default");
    }

    [Fact]
    public void AddProfile_IncreasesProfileCount()
    {
        var settings = new UserSettings();
        var profile = new Profile(Guid.NewGuid(), "Gaming");

        settings.AddProfile(profile);

        settings.Profiles.Should().HaveCount(2);
    }

    [Fact]
    public void SwitchProfile_ValidId_ChangesActiveProfile()
    {
        var settings = new UserSettings();
        var newProfile = new Profile(Guid.NewGuid(), "Gaming");
        settings.AddProfile(newProfile);

        settings.SwitchProfile(newProfile.ProfileId);

        settings.ActiveProfileId.Should().Be(newProfile.ProfileId);
        settings.GetActiveProfile().Name.Should().Be("Gaming");
    }

    [Fact]
    public void SwitchProfile_InvalidId_ThrowsInvalidOperationException()
    {
        var settings = new UserSettings();

        var act = () => settings.SwitchProfile(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>().WithMessage("*not found*");
    }

    [Fact]
    public void RemoveProfile_NonDefaultProfile_Removes()
    {
        var settings = new UserSettings();
        var profile = new Profile(Guid.NewGuid(), "Gaming");
        settings.AddProfile(profile);

        settings.RemoveProfile(profile.ProfileId);

        settings.Profiles.Should().ContainSingle();
    }

    [Fact]
    public void RemoveProfile_DefaultProfile_ThrowsInvalidOperationException()
    {
        var settings = new UserSettings();
        var defaultProfileId = settings.Profiles[0].ProfileId;

        var act = () => settings.RemoveProfile(defaultProfileId);

        act.Should().Throw<InvalidOperationException>().WithMessage("*default*");
    }

    [Fact]
    public void RemoveProfile_ActiveProfile_SwitchesToDefault()
    {
        var settings = new UserSettings();
        var profile = new Profile(Guid.NewGuid(), "Gaming");
        settings.AddProfile(profile);
        settings.SwitchProfile(profile.ProfileId);

        settings.RemoveProfile(profile.ProfileId);

        settings.ActiveProfileId.Should().Be(settings.Profiles.First(p => p.IsDefault).ProfileId);
    }

    [Fact]
    public void RemoveProfile_NonexistentId_DoesNotThrow()
    {
        var settings = new UserSettings();

        var act = () => settings.RemoveProfile(Guid.NewGuid());

        act.Should().NotThrow();
    }

    [Fact]
    public void UpdateAudio_ReplacesConfig()
    {
        var settings = new UserSettings();
        var newAudio = new AudioConfig(IsEnabled: false, Volume: 0.8f);

        settings.UpdateAudio(newAudio);

        settings.Audio.IsEnabled.Should().BeFalse();
        settings.Audio.Volume.Should().Be(0.8f);
    }

    [Fact]
    public void UpdateHistory_ReplacesConfig()
    {
        var settings = new UserSettings();
        var newHistory = new HistoryConfig(RetentionDays: 14, MaxEntries: 500);

        settings.UpdateHistory(newHistory);

        settings.History.RetentionDays.Should().Be(14);
        settings.History.MaxEntries.Should().Be(500);
    }
}
