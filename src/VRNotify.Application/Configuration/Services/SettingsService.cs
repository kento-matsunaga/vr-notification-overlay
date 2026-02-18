using Serilog;
using VRNotify.Domain.Configuration;
using VRNotify.Domain.NotificationProcessing;

namespace VRNotify.Application.Configuration.Services;

public sealed class SettingsService : ISettingsService
{
    private readonly ISettingsRepository _repository;
    private readonly ILogger _logger;

    public SettingsService(ISettingsRepository repository, ILogger logger)
    {
        _repository = repository;
        _logger = logger.ForContext<SettingsService>();
    }

    public Task<UserSettings> LoadAsync(CancellationToken ct = default)
        => _repository.LoadAsync(ct);

    public Task SaveAsync(UserSettings settings, CancellationToken ct = default)
        => _repository.SaveAsync(settings, ct);

    public async Task ToggleDndAsync(DndMode mode, CancellationToken ct = default)
    {
        var settings = await _repository.LoadAsync(ct);
        var profile = settings.GetActiveProfile();
        profile.UpdateDnd(new DndSettings(mode));
        await _repository.SaveAsync(settings, ct);
        _logger.Information("DND mode changed to {Mode}", mode);
    }

    public async Task RegisterAppAsync(string appName, CancellationToken ct = default)
    {
        var settings = await _repository.LoadAsync(ct);
        var profile = settings.GetActiveProfile();

        // Already registered — nothing to do
        if (profile.FilterRules.Any(r =>
            r.RuleType == FilterRuleType.AppName &&
            r.Parameters.TryGetValue("AppName", out var existing) &&
            string.Equals(existing, appName, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        // If any existing rule is Include → allowlist mode → new app defaults to Exclude
        // Otherwise (all Exclude or no rules) → blocklist mode → new app defaults to Include
        var condition = profile.FilterRules.Any(r => r.Condition == FilterCondition.Include)
            ? FilterCondition.Exclude
            : FilterCondition.Include;

        var rule = new FilterRule(
            Guid.NewGuid(),
            FilterRuleType.AppName,
            condition,
            new Dictionary<string, string> { ["AppName"] = appName },
            profile.FilterRules.Count);

        profile.AddFilterRule(rule);
        await _repository.SaveAsync(settings, ct);
        _logger.Information("Auto-registered app {AppName} with {Condition}", appName, condition);
    }
}
