using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;
using VRNotify.Domain.Configuration;

namespace VRNotify.Infrastructure.Persistence;

public sealed class JsonSettingsRepository : ISettingsRepository
{
    private static readonly string DefaultPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "VRNotify", "settings.json");

    private readonly string _filePath;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public JsonSettingsRepository(string? filePath = null, ILogger? logger = null)
    {
        _filePath = filePath ?? DefaultPath;
        _logger = logger ?? Log.ForContext<JsonSettingsRepository>();
    }

    public async Task<UserSettings> LoadAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            if (!File.Exists(_filePath))
            {
                _logger.Information("Settings file not found at {Path}, returning defaults", _filePath);
                return new UserSettings();
            }

            try
            {
                var json = await File.ReadAllTextAsync(_filePath, ct);
                var dto = JsonSerializer.Deserialize<SettingsDto>(json, JsonOptions);
                if (dto is null)
                    return new UserSettings();

                return dto.ToDomain();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to load settings from {Path}, returning defaults", _filePath);
                return new UserSettings();
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAsync(UserSettings settings, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var directory = Path.GetDirectoryName(_filePath)!;
            Directory.CreateDirectory(directory);

            var dto = SettingsDto.FromDomain(settings);
            var json = JsonSerializer.Serialize(dto, JsonOptions);

            // Atomic write: write to .tmp, then rename
            var tmpPath = _filePath + ".tmp";
            await File.WriteAllTextAsync(tmpPath, json, ct);
            File.Move(tmpPath, _filePath, overwrite: true);

            _logger.Debug("Settings saved to {Path}", _filePath);
        }
        finally
        {
            _lock.Release();
        }
    }
}
