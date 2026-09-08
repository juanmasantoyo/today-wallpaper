using SkiaSharp;
using TodayWallpaper.Core.Weather;

namespace TodayWallpaper.Core.Palette;

/// <summary>
/// Applies bounded, weather-modulated random jitter in HSL space to a palette color.
/// Lightness is clamped post-jitter to enforce the ≤ 55% contrast constraint.
/// </summary>
public static class ColorJitter
{
    /// <summary>
    /// Returns a jittered copy of <paramref name="baseColor"/>.
    /// The jitter ranges are modulated by weather parameters:
    /// <list type="bullet">
    ///   <item>Wind speed amplifies hue variation (gusty = wider color spread).</item>
    ///   <item>Stormy conditions add additional hue instability.</item>
    /// </list>
    /// </summary>
    public static SKColor Apply(SKColor baseColor, Random rng, WeatherData data)
    {
        float hueJitter        = rng.NextSingle() * 30f - 15f;   // ±15°
        float saturationJitter = rng.NextSingle() * 20f - 10f;   // ±10%
        float lightnessJitter  = rng.NextSingle() * 7f - 3.5f;   // ±3.5% (tightened to preserve vibrancy)

        // Wind amplifies hue spread — gusty days produce more colour variety.
        hueJitter *= 1f + (float)(data.WindspeedKmh / 80.0);

        // Stormy conditions push hue further from baseline.
        if (data.Condition == WeatherCondition.Stormy)
            hueJitter *= 1.5f;

        baseColor.ToHsl(out float h, out float s, out float l);
        h = (h + hueJitter + 360f) % 360f;
        s = Math.Clamp(s + saturationJitter, 35f, 95f);
        l = Math.Clamp(l + lightnessJitter, LuminanceValidator.MinLightness, LuminanceValidator.MaxLightness);

        return SKColor.FromHsl(h, s, l);
    }
}
