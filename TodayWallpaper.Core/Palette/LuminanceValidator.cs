using SkiaSharp;

namespace TodayWallpaper.Core.Palette;

/// <summary>
/// Validates palette colors to ensure sufficient contrast against desktop icons and text.
/// </summary>
public static class LuminanceValidator
{
    /// <summary>Minimum allowed HSL lightness (0-100 scale).</summary>
    public const float MinLightness = 20f;

    /// <summary>Maximum allowed HSL lightness (0-100 scale).</summary>
    public const float MaxLightness = 55f;

    /// <summary>Tolerance for 8-bit RGB color quantization rounding.</summary>
    private const float Tolerance = 0.5f;

    /// <summary>
    /// Returns <c>true</c> when the color's HSL lightness is between 20% and 55%.
    /// Colors below 20% become muddy or indistinguishable; colors above 55% reduce contrast of desktop icons and text.
    /// </summary>
    public static bool IsAcceptable(SKColor color)
    {
        color.ToHsl(out _, out _, out float lightness);
        return lightness >= (MinLightness - Tolerance) && lightness <= (MaxLightness + Tolerance);
    }
}
