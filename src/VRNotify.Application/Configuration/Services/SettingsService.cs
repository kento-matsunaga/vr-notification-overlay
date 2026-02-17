using Serilog;
using VRNotify.Domain.Configuration;

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
}
