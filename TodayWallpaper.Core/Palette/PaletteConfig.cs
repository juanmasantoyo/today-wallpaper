using TodayWallpaper.Core.Weather;

namespace TodayWallpaper.Core.Palette;

/// <summary>Full palette configuration loaded from <c>palettes.json</c>.</summary>
/// <param name="Empathic">Palettes for Empathic mode, keyed by <see cref="WeatherCondition"/>.</param>
/// <param name="Contrast">Palettes for Contrast mode, keyed by <see cref="WeatherCondition"/>.</param>
public record PaletteConfig(
    Dictionary<WeatherCondition, List<PaletteEntry>> Empathic,
    Dictionary<WeatherCondition, List<PaletteEntry>> Contrast);
