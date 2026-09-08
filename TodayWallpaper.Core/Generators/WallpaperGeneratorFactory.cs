using Microsoft.Extensions.Logging;
using TodayWallpaper.Core.Palette;
using TodayWallpaper.Core.Weather;

namespace TodayWallpaper.Core.Generators;

/// <summary>
/// Selects the appropriate <see cref="IWallpaperGenerator"/> based on the configured style.
/// When the style is <c>"Random"</c>, a generator is picked deterministically from the weather seed.
/// </summary>
public sealed class WallpaperGeneratorFactory(
    IEnumerable<IWallpaperGenerator> generators,
    ILogger<WallpaperGeneratorFactory> logger)
{
    private readonly IReadOnlyList<IWallpaperGenerator> _generators = generators.ToArray();

    /// <summary>All registered style IDs.</summary>
    public IReadOnlyList<string> StyleIds => _generators.Select(g => g.StyleId).ToList().AsReadOnly();

    /// <summary>
    /// Returns a generator chosen from <paramref name="selectedStyleIds"/>.
    /// If multiple styles are selected, one is picked deterministically using weather data (or tick count).
    /// If none is selected or matched, falls back to all available generators.
    /// </summary>
    public IWallpaperGenerator Resolve(IReadOnlyList<string>? selectedStyleIds, WeatherData? weather = null)
    {
        if (_generators.Count == 0)
        {
            throw new InvalidOperationException("No wallpaper generators are registered.");
        }

        var candidates = (selectedStyleIds is { Count: > 0 })
            ? _generators.Where(g => selectedStyleIds.Contains(g.StyleId, StringComparer.OrdinalIgnoreCase)).ToList()
            : _generators.ToList();

        if (candidates.Count == 0)
        {
            logger.LogWarning("None of the selected generator styles matched registered generators; falling back to all generators.");
            candidates = _generators.ToList();
        }

        if (candidates.Count == 1)
        {
            return candidates[0];
        }

        // Deterministic random selection among the selected styles from weather data.
        int seed = weather is not null ? WeatherSeed.Build(weather) : Environment.TickCount;
        int index = Math.Abs(seed) % candidates.Count;
        var chosen = candidates[index];

        logger.LogDebug("Random style selection among [{Styles}]: {Chosen}",
            string.Join(", ", candidates.Select(c => c.StyleId)), chosen.StyleId);

        return chosen;
    }

    /// <summary>
    /// Returns the generator for <paramref name="styleId"/>.
    /// If <paramref name="styleId"/> is <c>"Random"</c>, the selection is derived from all available generators.
    /// </summary>
    public IWallpaperGenerator Resolve(string styleId, WeatherData? weather = null)
    {
        if (string.Equals(styleId, "Random", StringComparison.OrdinalIgnoreCase))
        {
            return Resolve((IReadOnlyList<string>?)null, weather);
        }

        var exact = _generators.FirstOrDefault(g =>
            string.Equals(g.StyleId, styleId, StringComparison.OrdinalIgnoreCase));

        if (exact is not null) return exact;

        logger.LogWarning("Unknown generator style '{Style}'; falling back to random selection.", styleId);
        return Resolve((IReadOnlyList<string>?)null, weather);
    }
}
