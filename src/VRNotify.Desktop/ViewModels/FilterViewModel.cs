using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using VRNotify.Domain.Configuration;
using VRNotify.Domain.NotificationProcessing;

namespace VRNotify.Desktop.ViewModels;

public sealed partial class FilterViewModel : ObservableObject
{
    private readonly ISettingsRepository _settingsRepository;

    [ObservableProperty]
    private bool _isAllowlistMode;

    public ObservableCollection<AppFilterEntry> Apps { get; } = new();

    public FilterViewModel(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public void LoadFromProfile(Profile profile)
    {
        Apps.Clear();

        var hasInclude = profile.FilterRules.Any(r => r.Condition == FilterCondition.Include);
        IsAllowlistMode = hasInclude;

        foreach (var rule in profile.FilterRules.Where(r => r.RuleType == FilterRuleType.AppName))
        {
            if (rule.Parameters.TryGetValue("AppName", out var appName))
            {
                Apps.Add(new AppFilterEntry
                {
                    AppName = appName,
                    IsAllowed = rule.Condition == FilterCondition.Include,
                    RuleId = rule.RuleId
                });
            }
        }
    }

    public void ApplyToProfile(Profile profile)
    {
        // Remove existing AppName rules
        var existingRuleIds = profile.FilterRules
            .Where(r => r.RuleType == FilterRuleType.AppName)
            .Select(r => r.RuleId)
            .ToList();
        foreach (var ruleId in existingRuleIds)
            profile.RemoveFilterRule(ruleId);

        // Add updated rules
        int order = 1;
        foreach (var app in Apps)
        {
            var condition = app.IsAllowed ? FilterCondition.Include : FilterCondition.Exclude;
            var rule = new FilterRule(
                Guid.NewGuid(),
                FilterRuleType.AppName,
                condition,
                new Dictionary<string, string> { ["AppName"] = app.AppName },
                order++);
            profile.AddFilterRule(rule);
        }
    }

    [RelayCommand]
    private void ToggleAppFilter(AppFilterEntry entry)
    {
        entry.IsAllowed = !entry.IsAllowed;
    }

    public void RegisterApp(string appName)
    {
        if (Apps.Any(a => string.Equals(a.AppName, appName, StringComparison.OrdinalIgnoreCase)))
            return;

        Apps.Add(new AppFilterEntry
        {
            AppName = appName,
            IsAllowed = !IsAllowlistMode // In blocklist mode, new apps are allowed; in allowlist, they're blocked
        });
    }
}

public sealed partial class AppFilterEntry : ObservableObject
{
    [ObservableProperty]
    private string _appName = "";

    [ObservableProperty]
    private bool _isAllowed = true;

    public Guid RuleId { get; set; }
}
