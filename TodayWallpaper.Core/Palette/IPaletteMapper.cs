using TodayWallpaper.Core.Weather;

namespace TodayWallpaper.Core.Palette;

/// <summary>Maps a weather condition and palette mode to a ready-to-use <see cref="ColorPalette"/>.</summary>
public interface IPaletteMapper
{
    /// <summary>
    /// Returns a <see cref="ColorPalette"/> with weather-modulated HSL jitter applied.
    /// The same <paramref name="weather"/> snapshot always produces the same result.
    /// </summary>
    ColorPalette Map(WeatherCondition condition, PaletteMode mode, WeatherData weather);
}
