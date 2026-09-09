using Microsoft.Extensions.Logging;
using SkiaSharp;
using TodayWallpaper.Core.Palette;
using TodayWallpaper.Core.Weather;

namespace TodayWallpaper.Core.Generators;

/// <summary>
/// Generates a wallpaper from layered radial gradient shaders composited with blend modes.
/// Gradient count and placement respond to wind speed.
/// Dew point controls gradient smoothness (feathering radius).
/// </summary>
public sealed class RadialGradientGenerator(ILogger<RadialGradientGenerator> logger) : IWallpaperGenerator
{
    /// <inheritdoc/>
    public string StyleId => "RadialGradient";

    /// <inheritdoc/>
    public Task<string> GenerateAsync(
        ColorPalette palette,
        WeatherData weather,
        int width,
        int height,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("RadialGradient: generating {W}×{H}", width, height);
        var rng = new Random(WeatherSeed.Build(weather));

        int gradientCount = (int)Math.Clamp(3 + weather.WindspeedKmh / 30.0, 3, 6);
        float feather     = (float)Math.Clamp(0.6 + weather.DewpointCelsius / 60.0, 0.5, 1.0);

        var allColors = palette.PrimaryColors.Concat(palette.SecondaryColors).ToArray();

        var info    = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas  = surface.Canvas;

        // Background fill with gradient between primary colors for chromatic depth.
        using (var bgShader = SKShader.CreateLinearGradient(
            new SKPoint(0, 0),
            new SKPoint(width, height),
            [palette.PrimaryColors[0], palette.PrimaryColors[^1]],
            SKShaderTileMode.Clamp))
        using (var bgPaint = new SKPaint { Shader = bgShader, IsAntialias = true, IsDither = true })
        {
            canvas.DrawRect(SKRect.Create(width, height), bgPaint);
        }

        for (int i = 0; i < gradientCount; i++)
        {
            float cx     = rng.NextSingle() * width;
            float cy     = rng.NextSingle() * height;
            float radius = (rng.NextSingle() * 0.6f + 0.4f) * Math.Max(width, height) * feather;

            var innerColor = allColors[rng.Next(allColors.Length)];
            var outerColor = allColors[rng.Next(allColors.Length)].WithAlpha(0);

            using var shader = SKShader.CreateRadialGradient(
                new SKPoint(cx, cy),
                radius,
                [innerColor, outerColor],
                SKShaderTileMode.Clamp);

            // Use blend modes that preserve chromatic purity and avoid blowing out to white
            var blendMode = i % 2 == 0 ? SKBlendMode.SrcOver : SKBlendMode.SoftLight;

            using var paint = new SKPaint
            {
                Shader      = shader,
                BlendMode   = blendMode,
                IsAntialias = true,
                IsDither    = true
            };

            canvas.DrawRect(SKRect.Create(width, height), paint);
        }

        // Cloud-cover subtle atmospheric tint (soft, preserving color vibrancy).
        float darken = (float)Math.Clamp(weather.CloudCoverPercent / 2500.0, 0.0, 0.04);
        if (darken > 0.005f)
        {
            using var darkenPaint = new SKPaint
            {
                IsAntialias = true,
                Color = new SKColor(0, 0, 0, (byte)(darken * 255))
            };
            canvas.DrawRect(SKRect.Create(width, height), darkenPaint);
        }

        SaveImage(surface, outputPath);
        return Task.FromResult(outputPath);
    }

    private static void SaveImage(SKSurface surface, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var image = surface.Snapshot();
        var format = path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
            ? SKEncodedImageFormat.Jpeg
            : SKEncodedImageFormat.Png;
        using var data = image.Encode(format, 100);
        File.WriteAllBytes(path, data.ToArray());
    }
}
