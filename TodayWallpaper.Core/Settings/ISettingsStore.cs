namespace TodayWallpaper.Core.Settings;

/// <summary>Persists and retrieves application settings.</summary>
public interface ISettingsStore
{
    /// <summary>Loads settings from disk. Returns defaults if the file does not exist.</summary>
    Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists the given settings to disk.</summary>
    Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default);
}
