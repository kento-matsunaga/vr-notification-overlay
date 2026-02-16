using VRNotify.Domain.Configuration;

namespace VRNotify.Infrastructure.Persistence;

public sealed class JsonSettingsRepository : ISettingsRepository
{
    public Task<UserSettings> LoadAsync(CancellationToken ct = default)
        => throw new NotImplementedException();

    public Task SaveAsync(UserSettings settings, CancellationToken ct = default)
        => throw new NotImplementedException();
}
