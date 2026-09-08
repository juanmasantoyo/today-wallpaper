using Microsoft.Extensions.Logging;
using SkiaSharp;
using TodayWallpaper.Core.Palette;
using TodayWallpaper.Core.Weather;

namespace TodayWallpaper.Core.Generators;

/// <summary>
/// Generates a wallpaper by mapping 2-D fractal noise to a palette gradient.
/// Noise scale and detail level respond to weather parameters:
/// <list type="bullet">
///   <item>Low visibility → higher noise intensity (grain).</item>
///   <item>High humidity → softer transitions (fewer octaves).</item>
///   <item>Apparent temperature → palette hue offset (±15° HSL).</item>
/// </list>
/// </summary>
public sealed class PerlinFlowGenerator(ILogger<PerlinFlowGenerator> logger) : IWallpaperGenerator
{
    /// <inheritdoc/>
    public string StyleId => "PerlinFlow";

    /// <inheritdoc/>
    public Task<string> GenerateAsync(
        ColorPalette palette,
        WeatherData weather,
        int width,
        int height,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("PerlinFlow: generating {W}×{H}", width, height);
        var rng = new Random(WeatherSeed.Build(weather));

        // Weather-driven parameters.
        int octaves      = (int)Math.Clamp(8 - weather.Humidity / 20.0, 3, 8);
        float noiseScale = (float)Math.Clamp(1.5 + (50 - weather.VisibilityKm) / 50.0, 1.0, 3.5);
        float hueOffset  = (float)((weather.ApparentTemperatureCelsius - weather.TemperatureCelsius) * 1.5f);
        float offsetX    = (float)(rng.NextDouble() * 1000);
        float offsetY    = (float)(rng.NextDouble() * 1000);

        var allColors = palette.PrimaryColors.Concat(palette.SecondaryColors).ToArray();

        var info    = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        using var bitmap  = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

        // Fill each pixel from the noise field.
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = (x / (float)width)  * noiseScale + offsetX;
                float ny = (y / (float)height) * noiseScale + offsetY;
                float n  = NoiseHelper.FractalNoise(nx, ny, octaves);

                // Subtle triangular spatial dither (±0.5/255) to eliminate banding across continuous gradients
                float dither = ((float)rng.NextDouble() - 0.5f) * (1f / 255f);
                var color = GradientSample(allColors, Math.Clamp(n + dither, 0f, 0.9999f), hueOffset);
                bitmap.SetPixel(x, y, color);
            }
        }

        var canvas = surface.Canvas;
        using var paint = new SKPaint { IsAntialias = true, IsDither = true };
        canvas.DrawBitmap(bitmap, 0, 0, paint);
        SaveJpeg(surface, outputPath);
        return Task.FromResult(outputPath);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static SKColor GradientSample(SKColor[] colors, float t, float hueOffset)
    {
        t = Math.Clamp(t, 0f, 0.9999f);
        float scaled = t * (colors.Length - 1);
        int i = (int)scaled;
        float frac = scaled - i;

        var a = colors[i];
        var b = colors[Math.Min(i + 1, colors.Length - 1)];

        a.ToHsl(out float h1, out float s1, out float l1);
        b.ToHsl(out float h2, out float s2, out float l2);

        // Shortest circular hue arc
        float diff = (h2 - h1 + 360f) % 360f;
        if (diff > 180f) diff -= 360f;
        float h = (h1 + diff * frac + hueOffset + 360f) % 360f;

        // Smoothly interpolate natural saturation and bounded luminance across transitions
        float s = Math.Clamp(s1 + (s2 - s1) * frac, 0f, 75f);
        float l = Math.Clamp(l1 + (l2 - l1) * frac, LuminanceValidator.MinLightness, LuminanceValidator.MaxLightness);

        return SKColor.FromHsl(h, s, l);
    }

    private static void SaveJpeg(SKSurface surface, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var image = surface.Snapshot();
        using var data  = image.Encode(SKEncodedImageFormat.Jpeg, 100);
        File.WriteAllBytes(path, data.ToArray());
    }
}
