using VRNotify.Domain.Configuration;

namespace VRNotify.Application.Configuration.Services;

/// <summary>
/// Application service for settings operations.
/// Encapsulates load-modify-save workflows so that upper layers
/// (Desktop, Host) never touch ISettingsRepository directly.
/// </summary>
public interface ISettingsService
{
    Task<UserSettings> LoadAsync(CancellationToken ct = default);
    Task SaveAsync(UserSettings settings, CancellationToken ct = default);
    Task ToggleDndAsync(DndMode mode, CancellationToken ct = default);
}
