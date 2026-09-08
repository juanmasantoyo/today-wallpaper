using TodayWallpaper.Core.Weather;

namespace TodayWallpaper.Core.Palette;

/// <summary>
/// Applies jitter to palette entries and returns a <see cref="ColorPalette"/>
/// keyed to the given condition and mode, selecting randomly among enabled palettes.
/// </summary>
public sealed class PaletteMapper(IPaletteConfigLoader configLoader) : IPaletteMapper
{
    /// <inheritdoc/>
    public ColorPalette Map(WeatherCondition condition, PaletteMode mode, WeatherData weather)
    {
        var config = configLoader.Load();

        var section = mode == PaletteMode.Empathic ? config.Empathic : config.Contrast;
        var entries = section.TryGetValue(condition, out var found) && found.Count > 0
            ? found
            : [.. (mode == PaletteMode.Empathic ? configLoader.GetDefaultConfig().Empathic : configLoader.GetDefaultConfig().Contrast)[condition]];

        var rng = new Random(WeatherSeed.Build(weather));

        // Filter for enabled palettes
        var enabledEntries = entries.Where(e => e.IsEnabled).ToList();
        var selectedEntry = enabledEntries.Count > 0
            ? enabledEntries[rng.Next(enabledEntries.Count)]
            : entries.FirstOrDefault() ?? new PaletteEntry("Default", true, [new SkiaSharp.SKColor(50, 70, 90)], [new SkiaSharp.SKColor(30, 40, 60)]);

        var primary   = selectedEntry.Primary.Select(c => ColorJitter.Apply(c, rng, weather)).ToList().AsReadOnly();
        var secondary = selectedEntry.Secondary.Select(c => ColorJitter.Apply(c, rng, weather)).ToList().AsReadOnly();

        // Normalise temperature to [0, 1] over a sensible range (−10 °C to 40 °C).
        float temperature = (float)Math.Clamp((weather.TemperatureCelsius + 10.0) / 50.0, 0.0, 1.0);

        return new ColorPalette(primary, secondary, temperature);
    }
}
