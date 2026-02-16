namespace VRNotify.Domain.Configuration;

public interface ISettingsRepository
{
    Task<UserSettings> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(UserSettings settings, CancellationToken ct = default);
}
