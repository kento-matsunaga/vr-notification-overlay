using VRNotify.Domain.Configuration;
using VRNotify.Domain.NotificationProcessing;
using VRNotify.Domain.VRDisplay;

namespace VRNotify.Infrastructure.Persistence;

internal sealed class SettingsDto
{
    public string SchemaVersion { get; set; } = "1.0";
    public Guid ActiveProfileId { get; set; }
    public AudioConfigDto Audio { get; set; } = new();
    public HistoryConfigDto History { get; set; } = new();
    public List<ProfileDto> Profiles { get; set; } = new();

    public static SettingsDto FromDomain(UserSettings settings) => new()
    {
        SchemaVersion = settings.SchemaVersion,
        ActiveProfileId = settings.ActiveProfileId,
        Audio = AudioConfigDto.FromDomain(settings.Audio),
        History = HistoryConfigDto.FromDomain(settings.History),
        Profiles = settings.Profiles.Select(ProfileDto.FromDomain).ToList()
    };

    public UserSettings ToDomain()
    {
        var profiles = Profiles.Select(p => p.ToDomain());
        return new UserSettings(ActiveProfileId, Audio.ToDomain(), History.ToDomain(), profiles);
    }
}

internal sealed class AudioConfigDto
{
    public bool IsEnabled { get; set; } = true;
    public float Volume { get; set; } = 0.3f;

    public static AudioConfigDto FromDomain(AudioConfig config) => new()
    {
        IsEnabled = config.IsEnabled,
        Volume = config.Volume
    };

    public AudioConfig ToDomain() => new(IsEnabled, Volume);
}

internal sealed class HistoryConfigDto
{
    public int RetentionDays { get; set; } = 7;
    public int MaxEntries { get; set; } = 1000;

    public static HistoryConfigDto FromDomain(HistoryConfig config) => new()
    {
        RetentionDays = config.RetentionDays,
        MaxEntries = config.MaxEntries
    };

    public HistoryConfig ToDomain() => new(RetentionDays, MaxEntries);
}

internal sealed class ProfileDto
{
    public Guid ProfileId { get; set; }
    public string Name { get; set; } = "";
    public bool IsDefault { get; set; }
    public DisplayConfigDto Display { get; set; } = new();
    public DndSettingsDto Dnd { get; set; } = new();
    public List<FilterRuleDto> FilterRules { get; set; } = new();
    public List<Guid> EnabledSourceIds { get; set; } = new();

    public static ProfileDto FromDomain(Profile profile) => new()
    {
        ProfileId = profile.ProfileId,
        Name = profile.Name,
        IsDefault = profile.IsDefault,
        Display = DisplayConfigDto.FromDomain(profile.Display),
        Dnd = DndSettingsDto.FromDomain(profile.Dnd),
        FilterRules = profile.FilterRules.Select(FilterRuleDto.FromDomain).ToList(),
        EnabledSourceIds = profile.EnabledSourceIds.ToList()
    };

    public Profile ToDomain()
    {
        var rules = FilterRules.Select(r => r.ToDomain());
        return new Profile(ProfileId, Name, IsDefault, Display.ToDomain(), Dnd.ToDomain(), rules, EnabledSourceIds);
    }
}

internal sealed class DisplayConfigDto
{
    public DisplayPosition Position { get; set; } = DisplayPosition.HmdTop;
    public int SlotCount { get; set; } = 3;
    public float Opacity { get; set; } = 1.0f;
    public float Scale { get; set; } = 1.0f;
    public double? HighPriorityDurationSeconds { get; set; }
    public double? MediumPriorityDurationSeconds { get; set; }
    public double? LowPriorityDurationSeconds { get; set; }

    public static DisplayConfigDto FromDomain(DisplayConfig config) => new()
    {
        Position = config.Position,
        SlotCount = config.SlotCount,
        Opacity = config.Opacity,
        Scale = config.Scale,
        HighPriorityDurationSeconds = config.HighPriorityDuration?.TotalSeconds,
        MediumPriorityDurationSeconds = config.MediumPriorityDuration?.TotalSeconds,
        LowPriorityDurationSeconds = config.LowPriorityDuration?.TotalSeconds
    };

    public DisplayConfig ToDomain() => new(
        Position,
        SlotCount,
        Opacity,
        Scale,
        HighPriorityDurationSeconds.HasValue ? TimeSpan.FromSeconds(HighPriorityDurationSeconds.Value) : null,
        MediumPriorityDurationSeconds.HasValue ? TimeSpan.FromSeconds(MediumPriorityDurationSeconds.Value) : null,
        LowPriorityDurationSeconds.HasValue ? TimeSpan.FromSeconds(LowPriorityDurationSeconds.Value) : null);
}

internal sealed class DndSettingsDto
{
    public DndMode Mode { get; set; } = DndMode.Off;

    public static DndSettingsDto FromDomain(DndSettings settings) => new()
    {
        Mode = settings.Mode
    };

    public DndSettings ToDomain() => new(Mode);
}

internal sealed class FilterRuleDto
{
    public Guid RuleId { get; set; }
    public FilterRuleType RuleType { get; set; }
    public FilterCondition Condition { get; set; }
    public Dictionary<string, string> Parameters { get; set; } = new();
    public int Order { get; set; }

    public static FilterRuleDto FromDomain(FilterRule rule) => new()
    {
        RuleId = rule.RuleId,
        RuleType = rule.RuleType,
        Condition = rule.Condition,
        Parameters = rule.Parameters.ToDictionary(p => p.Key, p => p.Value),
        Order = rule.Order
    };

    public FilterRule ToDomain() => new(RuleId, RuleType, Condition, Parameters, Order);
}
