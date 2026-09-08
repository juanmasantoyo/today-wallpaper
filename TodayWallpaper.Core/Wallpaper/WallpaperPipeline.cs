using Microsoft.Extensions.Logging;
using TodayWallpaper.Core.Generators;
using TodayWallpaper.Core.History;
using TodayWallpaper.Core.Palette;
using TodayWallpaper.Core.Settings;
using TodayWallpaper.Core.Weather;

namespace TodayWallpaper.Core.Wallpaper;

/// <summary>
/// Orchestrates the full wallpaper generation pipeline:
/// Geolocation → Weather → Palette → Generate → Set → History → Cache.
/// </summary>
public class WallpaperPipeline(
    IGeolocationService geolocationService,
    IWeatherService weatherService,
    IPaletteMapper paletteMapper,
    WallpaperGeneratorFactory generatorFactory,
    IWallpaperSetter wallpaperSetter,
    IScreenResolutionProvider screenResolution,
    IHistoryStore historyStore,
    ISettingsStore settingsStore,
    ILogger<WallpaperPipeline> logger)
{
    /// <summary>
    /// Executes the pipeline.
    /// </summary>
    /// <param name="force">When true, bypasses enabled and expiration checks.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if a new wallpaper was generated and applied; otherwise <c>false</c>.</returns>
    public virtual async Task<bool> RunAsync(bool force = false, CancellationToken cancellationToken = default)
    {
        var settings = await settingsStore.LoadAsync(cancellationToken);

        if (!force && !settings.IsEnabled)
        {
            logger.LogInformation("TodayWallpaper is paused (IsEnabled=false). Exiting without change.");
            return false;
        }

        if (!force && !settings.IsExpired())
        {
            logger.LogInformation("TodayWallpaper: current wallpaper has not expired yet. Skipping generation.");
            return false;
        }

        // 1. Resolve location.
        logger.LogInformation("Step 1/5 — Resolving location...");
        var location = (settings.Location is not null)
            ? new LocationInfo(settings.Location.Latitude, settings.Location.Longitude, settings.Location.City ?? "Manual", "")
            : await geolocationService.GetLocationAsync(cancellationToken);

        // 2. Fetch weather.
        logger.LogInformation("Step 2/5 — Fetching weather for {City} ({Lat:F2}, {Lon:F2})...",
            location.City, location.Latitude, location.Longitude);
        var weather = await weatherService.GetCurrentWeatherAsync(
            location.Latitude, location.Longitude, location.City, cancellationToken);

        // 3. Build palette.
        logger.LogInformation("Step 3/5 — Building palette ({Mode})...", settings.PaletteMode);
        var palette = paletteMapper.Map(weather.Condition, settings.PaletteMode, weather);

        // 4. Generate image.
        logger.LogInformation("Step 4/5 — Generating wallpaper from selected styles: [{Styles}]...",
            string.Join(", ", settings.SelectedGeneratorStyles));
        var generator = generatorFactory.Resolve(settings.SelectedGeneratorStyles, weather);
        int width = screenResolution.GetWidth();
        int height = screenResolution.GetHeight();

        var timestamp = DateTime.Now;
        var fileName = $"history/{timestamp:yyyyMMdd_HHmm}_{weather.Condition}.jpg";
        var fullPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TodayWallpaper", fileName);

        var outputPath = await generator.GenerateAsync(palette, weather, width, height, fullPath, cancellationToken);

        // Copy to current_wallpaper.jpg for quick re-application.
        var currentPath = WallpaperSetter.CurrentWallpaperPath;
        var currentDir = Path.GetDirectoryName(currentPath);
        if (!string.IsNullOrEmpty(currentDir))
        {
            Directory.CreateDirectory(currentDir);
        }
        File.Copy(outputPath, currentPath, overwrite: true);

        // 5. Apply and record.
        logger.LogInformation("Step 5/5 — Applying wallpaper and recording history...");
        wallpaperSetter.Set(outputPath);

        var entry = new HistoryEntry(
            Id:          timestamp.ToString("o"),
            File:        fileName,
            Condition:   weather.Condition,
            Temperature: weather.TemperatureCelsius,
            Style:       generator.StyleId);

        await historyStore.AddAsync(entry, settings.HistoryMaxCount, cancellationToken);

        // 6. Update settings with cached weather and last generation time.
        var updatedSettings = settings with
        {
            LastGenerationTimeUtc = DateTime.UtcNow,
            CachedWeatherData = weather,
            CachedLocation = location
        };
        await settingsStore.SaveAsync(updatedSettings, cancellationToken);

        logger.LogInformation(
            "Wallpaper updated successfully: {Condition} / {Style} / {Temp:F1}°C at {Location}.",
            weather.Condition, generator.StyleId, weather.TemperatureCelsius, location.City);

        return true;
    }
}
