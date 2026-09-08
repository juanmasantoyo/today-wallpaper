using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace TodayWallpaper.Core.Settings;

/// <summary>
/// Reads and writes <see cref="AppSettings"/> from
/// <c>%APPDATA%\TodayWallpaper\settings.json</c>.
/// </summary>
public sealed class SettingsStore : ISettingsStore
{
    /// <summary>Default path to the settings file.</summary>
    public static readonly string DefaultFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TodayWallpaper",
        "settings.json");

    private readonly string _filePath;
    private readonly ILogger<SettingsStore> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    /// <summary>Initialises with default APPDATA file path.</summary>
    public SettingsStore(ILogger<SettingsStore> logger)
        : this(DefaultFilePath, logger)
    {
    }

    /// <summary>Initialises with a custom file path (useful for testing).</summary>
    public SettingsStore(string filePath, ILogger<SettingsStore> logger)
    {
        _filePath = filePath;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            _logger.LogDebug("Settings file not found at {Path}; using defaults.", _filePath);
            return new AppSettings();
        }

        try
        {
            await using var stream = File.OpenRead(_filePath);
            var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions, cancellationToken);
            return settings ?? new AppSettings();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize settings from {Path}; using defaults.", _filePath);
            return new AppSettings();
        }
    }

    /// <inheritdoc/>
    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, cancellationToken);
        _logger.LogDebug("Settings saved to {Path}.", _filePath);
    }
}
