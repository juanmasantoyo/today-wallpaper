using Microsoft.Extensions.Logging;
using SkiaSharp;
using TodayWallpaper.Core.Palette;
using TodayWallpaper.Core.Weather;

namespace TodayWallpaper.Core.Generators;

/// <summary>
/// Generates a wallpaper from translucent overlapping geometric primitives
/// (circles, rectangles, triangles, hexagons) with subtle drop shadows.
/// Shape count and size are driven by wind speed.
/// </summary>
public sealed class GeometricShapesGenerator(ILogger<GeometricShapesGenerator> logger) : IWallpaperGenerator
{
    /// <inheritdoc/>
    public string StyleId => "GeometricShapes";

    /// <inheritdoc/>
    public Task<string> GenerateAsync(
        ColorPalette palette,
        WeatherData weather,
        int width,
        int height,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("GeometricShapes: generating {W}×{H}", width, height);
        var rng = new Random(WeatherSeed.Build(weather));

        int shapeCount = (int)Math.Clamp(20 + weather.WindspeedKmh * 0.4, 20, 55);
        float darken   = (float)Math.Clamp(weather.CloudCoverPercent / 2500.0, 0.0, 0.04);

        var allColors = palette.PrimaryColors.Concat(palette.SecondaryColors).ToArray();

        var info    = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas  = surface.Canvas;

        // Background gradient.
        DrawBackground(canvas, palette, width, height);

        for (int i = 0; i < shapeCount; i++)
        {
            var color   = allColors[rng.Next(allColors.Length)];
            byte alpha  = (byte)(rng.Next(115, 215)); // 45-85% opacity for rich, vivid color presence
            var cx      = rng.NextSingle() * width;
            var cy      = rng.NextSingle() * height;
            float size  = rng.NextSingle() * (height / 4f) + height / 8f;
            float angle = rng.NextSingle() * 360f;
            int shapeType = rng.Next(4); // 0=circle, 1=rect, 2=triangle, 3=hexagon

            using var shadow = new SKPaint
            {
                IsAntialias = true,
                IsDither    = true,
                Color       = color.WithAlpha(alpha),
                ImageFilter = SKImageFilter.CreateDropShadow(4, 4, 8, 8, new SKColor(0, 0, 0, 35))
            };

            canvas.Save();
            canvas.Translate(cx, cy);
            canvas.RotateDegrees(angle);

            DrawShape(canvas, shapeType, size, shadow);

            canvas.Restore();
            shadow.Dispose();
        }

        // Cloud-cover subtle atmospheric tint (soft, preserving color vibrancy).
        if (darken > 0.005f)
        {
            using var darkenPaint = new SKPaint
            {
                IsAntialias = true,
                Color = new SKColor(0, 0, 0, (byte)(darken * 255))
            };
            canvas.DrawRect(SKRect.Create(width, height), darkenPaint);
        }

        SaveJpeg(surface, outputPath);
        return Task.FromResult(outputPath);
    }

    private static void DrawBackground(SKCanvas canvas, ColorPalette palette, int width, int height)
    {
        var c1 = palette.PrimaryColors[0];
        var c2 = palette.PrimaryColors[^1];
        using var shader = SKShader.CreateLinearGradient(
            new SKPoint(0, 0), new SKPoint(width, height),
            [c1, c2], SKShaderTileMode.Clamp);
        using var bg = new SKPaint { Shader = shader, IsAntialias = true, IsDither = true };
        canvas.DrawRect(SKRect.Create(width, height), bg);
    }

    private static void DrawShape(SKCanvas canvas, int shapeType, float size, SKPaint paint)
    {
        switch (shapeType)
        {
            case 0: // Circle
                canvas.DrawCircle(0, 0, size / 2, paint);
                break;

            case 1: // Rectangle
                canvas.DrawRect(SKRect.Create(-size / 2, -size / 3, size, size * 0.6f), paint);
                break;

            case 2: // Triangle
                using (var path = new SKPath())
                {
                    path.MoveTo(0, -size / 2);
                    path.LineTo(size / 2, size / 2);
                    path.LineTo(-size / 2, size / 2);
                    path.Close();
                    canvas.DrawPath(path, paint);
                }
                break;

            case 3: // Hexagon
                using (var path = BuildHexagon(size / 2))
                {
                    canvas.DrawPath(path, paint);
                }
                break;
        }
    }

    private static SKPath BuildHexagon(float r)
    {
        var path = new SKPath();
        for (int i = 0; i < 6; i++)
        {
            float a = (float)(i * Math.PI / 3.0);
            var pt = new SKPoint(r * MathF.Cos(a), r * MathF.Sin(a));
            if (i == 0) path.MoveTo(pt);
            else        path.LineTo(pt);
        }
        path.Close();
        return path;
    }

    private static void SaveJpeg(SKSurface surface, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var image = surface.Snapshot();
        using var data  = image.Encode(SKEncodedImageFormat.Jpeg, 100);
        File.WriteAllBytes(path, data.ToArray());
    }
}
