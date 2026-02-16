using VRNotify.Domain.Common;

namespace VRNotify.Domain.Configuration;

public sealed class UserSettings : Entity
{
    public string SchemaVersion { get; } = "1.0";
    public Guid ActiveProfileId { get; private set; }
    public AudioConfig Audio { get; private set; }
    public HistoryConfig History { get; private set; }
    private readonly List<Profile> _profiles = new();
    public IReadOnlyList<Profile> Profiles => _profiles.AsReadOnly();

    public UserSettings()
    {
        var defaultProfile = new Profile(Guid.NewGuid(), "Default", isDefault: true);
        _profiles.Add(defaultProfile);
        ActiveProfileId = defaultProfile.ProfileId;
        Audio = new AudioConfig();
        History = new HistoryConfig();
    }

    public Profile GetActiveProfile() =>
        _profiles.First(p => p.ProfileId == ActiveProfileId);

    public void SwitchProfile(Guid profileId)
    {
        if (_profiles.All(p => p.ProfileId != profileId))
            throw new InvalidOperationException($"Profile {profileId} not found");
        ActiveProfileId = profileId;
    }

    public void AddProfile(Profile profile) => _profiles.Add(profile);

    public void RemoveProfile(Guid profileId)
    {
        var profile = _profiles.FirstOrDefault(p => p.ProfileId == profileId);
        if (profile is null) return;
        if (profile.IsDefault)
            throw new InvalidOperationException("Cannot delete the default profile");
        _profiles.Remove(profile);
        if (ActiveProfileId == profileId)
            ActiveProfileId = _profiles.First(p => p.IsDefault).ProfileId;
    }

    public void UpdateAudio(AudioConfig config) => Audio = config;
    public void UpdateHistory(HistoryConfig config) => History = config;

    /// <summary>
    /// Reconstitution constructor for persistence. Do not use for new settings.
    /// </summary>
    internal UserSettings(
        Guid activeProfileId,
        AudioConfig audio,
        HistoryConfig history,
        IEnumerable<Profile> profiles)
    {
        ActiveProfileId = activeProfileId;
        Audio = audio;
        History = history;
        _profiles.AddRange(profiles);
    }
}
