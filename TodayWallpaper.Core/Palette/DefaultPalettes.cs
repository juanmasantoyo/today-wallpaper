using SkiaSharp;
using TodayWallpaper.Core.Weather;

namespace TodayWallpaper.Core.Palette;

/// <summary>
/// Provides hardcoded default palettes used as fallback when <c>palettes.json</c>
/// is absent, invalid, or contains out-of-range colors.
/// All default colors satisfy 20% ≤ HSL Lightness ≤ 55%.
/// </summary>
internal static class DefaultPalettes
{
    // ---------- helpers ----------
    private static SKColor C(string hex)
    {
        SKColor.TryParse(hex, out var c);
        return c;
    }

    private static PaletteEntry E(string name, string[] primary, string[] secondary)
        => new(name,
               true,
               primary.Select(C).ToList().AsReadOnly(),
               secondary.Select(C).ToList().AsReadOnly());

    // ---------- empathic ----------
    private static readonly Dictionary<WeatherCondition, List<PaletteEntry>> EmpathicEntries = new()
    {
        [WeatherCondition.Sunny]  = [E("Default", ["#C8860A", "#266B38", "#4E7C28"], ["#1E522C", "#B05000", "#7A3E12"])],
        [WeatherCondition.Cloudy] = [E("Default", ["#3A4A5C", "#2E3D52", "#4A5C6B"], ["#3D2E5C", "#4A3C30"])],
        [WeatherCondition.Rainy]  = [E("Default", ["#235874", "#2D7255", "#3D4E6D"], ["#4E4860", "#1C4456", "#366650"])],
        [WeatherCondition.Stormy] = [E("Default", ["#32386A", "#463366", "#23495D"], ["#245258", "#382C52"])],
        [WeatherCondition.Snowy]  = [E("Default", ["#2C3E55", "#244B6E", "#334B6E"], ["#424266", "#303058"])],
        [WeatherCondition.Foggy]  = [E("Default", ["#3A3A50", "#4E3A4E", "#3A4E4E"], ["#2E2E44", "#3E2E3E"])]
    };

    // ---------- contrast (mood-lifting opposites) ----------
    private static readonly Dictionary<WeatherCondition, List<PaletteEntry>> ContrastEntries = new()
    {
        [WeatherCondition.Sunny]  = [E("Default", ["#235874", "#2D7255", "#3D4E6D"], ["#4E4860", "#1C4456", "#366650"])],
        [WeatherCondition.Cloudy] = [E("Default", ["#C8860A", "#B05000", "#8B3500"], ["#7A2200", "#663608"])],
        [WeatherCondition.Rainy]  = [E("Default", ["#C8860A", "#266B38", "#4E7C28"], ["#1E522C", "#B05000", "#7A3E12"])],
        [WeatherCondition.Stormy] = [E("Default", ["#C8860A", "#A06000", "#886030"], ["#6B3A00", "#663608"])],
        [WeatherCondition.Snowy]  = [E("Default", ["#7A3500", "#A05500", "#8B4020"], ["#70300A", "#682C08"])],
        [WeatherCondition.Foggy]  = [E("Default", ["#C8860A", "#B07020", "#906000"], ["#6B4000", "#663608"])]
    };

    /// <summary>Default <see cref="PaletteConfig"/> with all conditions covered.</summary>
    public static readonly PaletteConfig Config = new(
        EmpathicEntries.ToDictionary(k => k.Key, v => v.Value.Select(e => e).ToList()),
        ContrastEntries.ToDictionary(k => k.Key, v => v.Value.Select(e => e).ToList()));

    /// <summary>JSON representation of the default palettes (written to disk on first run).</summary>
    public static readonly string Json = """
        {
          "empathic": {
            "Sunny":  [ { "name": "Default", "isEnabled": true, "primary": ["#C8860A", "#266B38", "#4E7C28"], "secondary": ["#1E522C", "#B05000", "#7A3E12"] } ],
            "Cloudy": [ { "name": "Default", "isEnabled": true, "primary": ["#3A4A5C", "#2E3D52", "#4A5C6B"], "secondary": ["#3D2E5C", "#4A3C30"] } ],
            "Rainy":  [ { "name": "Default", "isEnabled": true, "primary": ["#235874", "#2D7255", "#3D4E6D"], "secondary": ["#4E4860", "#1C4456", "#366650"] } ],
            "Stormy": [ { "name": "Default", "isEnabled": true, "primary": ["#32386A", "#463366", "#23495D"], "secondary": ["#245258", "#382C52"] } ],
            "Snowy":  [ { "name": "Default", "isEnabled": true, "primary": ["#2C3E55", "#244B6E", "#334B6E"], "secondary": ["#424266", "#303058"] } ],
            "Foggy":  [ { "name": "Default", "isEnabled": true, "primary": ["#3A3A50", "#4E3A4E", "#3A4E4E"], "secondary": ["#2E2E44", "#3E2E3E"] } ]
          },
          "contrast": {
            "Sunny":  [ { "name": "Default", "isEnabled": true, "primary": ["#235874", "#2D7255", "#3D4E6D"], "secondary": ["#4E4860", "#1C4456", "#366650"] } ],
            "Cloudy": [ { "name": "Default", "isEnabled": true, "primary": ["#C8860A", "#B05000", "#8B3500"], "secondary": ["#7A2200", "#663608"] } ],
            "Rainy":  [ { "name": "Default", "isEnabled": true, "primary": ["#C8860A", "#266B38", "#4E7C28"], "secondary": ["#1E522C", "#B05000", "#7A3E12"] } ],
            "Stormy": [ { "name": "Default", "isEnabled": true, "primary": ["#C8860A", "#A06000", "#886030"], "secondary": ["#6B3A00", "#663608"] } ],
            "Snowy":  [ { "name": "Default", "isEnabled": true, "primary": ["#7A3500", "#A05500", "#8B4020"], "secondary": ["#70300A", "#682C08"] } ],
            "Foggy":  [ { "name": "Default", "isEnabled": true, "primary": ["#C8860A", "#B07020", "#906000"], "secondary": ["#6B4000", "#663608"] } ]
          }
        }
        """;
}
