using Microsoft.Extensions.Logging;
using SkiaSharp;
using TodayWallpaper.Core.Palette;
using TodayWallpaper.Core.Weather;

namespace TodayWallpaper.Core.Generators;

/// <summary>
/// Generates a wallpaper from overlapping Gaussian-blurred colored blobs.
/// Wind speed controls blob count and size; humidity controls blur sigma.
/// A secondary translucent pass adds visual depth.
/// </summary>
public sealed class BlurBlobsGenerator(ILogger<BlurBlobsGenerator> logger) : IWallpaperGenerator
{
    /// <inheritdoc/>
    public string StyleId => "BlurBlobs";

    /// <inheritdoc/>
    public Task<string> GenerateAsync(
        ColorPalette palette,
        WeatherData weather,
        int width,
        int height,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("BlurBlobs: generating {W}×{H}", width, height);

        var rng = new Random(WeatherSeed.Build(weather));

        // Modulate parameters from weather data.
        int blobCount  = (int)Math.Clamp(8 + weather.WindspeedKmh / 10.0, 8, 20);
        float blurSigma = (float)Math.Clamp(60 + weather.Humidity * 0.6, 60, 120);
        float secondaryOpacity = (float)Math.Clamp(weather.PrecipitationMm / 50.0, 0.15, 0.55);
        float brightnessScale = (float)Math.Clamp(1.0 - weather.CloudCoverPercent / 500.0, 0.8, 1.0);

        var info    = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas  = surface.Canvas;

        // Background fill with gradient between primary colors for chromatic richness.
        using (var bgShader = SKShader.CreateLinearGradient(
            new SKPoint(0, 0),
            new SKPoint(width, height),
            [palette.PrimaryColors[0], palette.PrimaryColors[^1]],
            SKShaderTileMode.Clamp))
        using (var bgPaint = new SKPaint { Shader = bgShader, IsAntialias = true, IsDither = true })
        {
            canvas.DrawRect(SKRect.Create(width, height), bgPaint);
        }

        // Primary blob pass.
        DrawBlobPass(canvas, palette.PrimaryColors, blobCount, blurSigma, 1.0f, rng, width, height);

        // Secondary overlay pass (denser when precipitation is high).
        DrawBlobPass(canvas, palette.SecondaryColors, blobCount / 2 + 2, blurSigma * 0.7f,
                     secondaryOpacity, rng, width, height);

        // Cloud-cover subtle atmospheric tint (soft, preserving color vibrancy).
        if (brightnessScale < 0.98f)
        {
            using var darken = new SKPaint
            {
                IsAntialias = true,
                Color = new SKColor(0, 0, 0, (byte)((1f - brightnessScale) * 35))
            };
            canvas.DrawRect(SKRect.Create(width, height), darken);
        }

        SaveJpeg(surface, outputPath);
        return Task.FromResult(outputPath);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private static void DrawBlobPass(
        SKCanvas canvas,
        IReadOnlyList<SKColor> colors,
        int count,
        float sigma,
        float opacity,
        Random rng,
        int width,
        int height)
    {
        for (int i = 0; i < count; i++)
        {
            var color  = colors[rng.Next(colors.Count)];
            float radius = rng.NextSingle() * (height / 1.5f - height / 4f) + height / 4f;
            float cx   = rng.NextSingle() * width;
            float cy   = rng.NextSingle() * height;

            var paint = new SKPaint
            {
                IsAntialias = true,
                IsDither    = true,
                Color       = color.WithAlpha((byte)(255 * opacity)),
                ImageFilter = SKImageFilter.CreateBlur(sigma, sigma)
            };

            canvas.DrawCircle(cx, cy, radius, paint);
            paint.Dispose();
        }
    }

    private static void SaveJpeg(SKSurface surface, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var image = surface.Snapshot();
        using var data  = image.Encode(SKEncodedImageFormat.Jpeg, 100);
        File.WriteAllBytes(path, data.ToArray());
    }
}
