using VRNotify.Domain.Common;
using VRNotify.Domain.NotificationProcessing;

namespace VRNotify.Domain.Configuration;

public sealed class Profile : Entity
{
    public Guid ProfileId { get; }
    public string Name { get; private set; }
    public bool IsDefault { get; }
    public DisplayConfig Display { get; private set; }
    public DndSettings Dnd { get; private set; }
    private readonly List<FilterRule> _filterRules = new();
    public IReadOnlyList<FilterRule> FilterRules => _filterRules.AsReadOnly();
    private readonly List<Guid> _enabledSourceIds = new();
    public IReadOnlyList<Guid> EnabledSourceIds => _enabledSourceIds.AsReadOnly();

    public Profile(Guid profileId, string name, bool isDefault = false)
    {
        ProfileId = profileId;
        Name = name;
        IsDefault = isDefault;
        Display = new DisplayConfig();
        Dnd = new DndSettings();
    }

    public void UpdateName(string name) => Name = name;
    public void UpdateDisplay(DisplayConfig config) => Display = config;
    public void UpdateDnd(DndSettings settings) => Dnd = settings;

    public void AddFilterRule(FilterRule rule) => _filterRules.Add(rule);
    public void RemoveFilterRule(Guid ruleId) => _filterRules.RemoveAll(r => r.RuleId == ruleId);

    public void EnableSource(Guid sourceId)
    {
        if (!_enabledSourceIds.Contains(sourceId))
            _enabledSourceIds.Add(sourceId);
    }

    public void DisableSource(Guid sourceId) => _enabledSourceIds.Remove(sourceId);

    /// <summary>
    /// Reconstitution constructor for persistence. Do not use for new profiles.
    /// </summary>
    internal Profile(
        Guid profileId,
        string name,
        bool isDefault,
        DisplayConfig display,
        DndSettings dnd,
        IEnumerable<FilterRule> filterRules,
        IEnumerable<Guid> enabledSourceIds)
    {
        ProfileId = profileId;
        Name = name;
        IsDefault = isDefault;
        Display = display;
        Dnd = dnd;
        _filterRules.AddRange(filterRules);
        _enabledSourceIds.AddRange(enabledSourceIds);
    }
}
