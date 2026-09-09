using Microsoft.Extensions.Logging;
using SkiaSharp;
using TodayWallpaper.Core.Palette;
using TodayWallpaper.Core.Weather;

namespace TodayWallpaper.Core.Generators;

/// <summary>
/// Generates an ethereal wallpaper composed of flowing, multi-layered undulating ribbons
/// (Aurora Borealis / atmospheric wave currents) rendered with smooth continuous cubic Bézier splines
/// and harmonic gradient shaders.
/// Wind speed modulates wave frequency and amplitude; humidity controls diffusion and atmospheric glow.
/// </summary>
public sealed class AuroraWavesGenerator(ILogger<AuroraWavesGenerator> logger) : IWallpaperGenerator
{
    /// <inheritdoc/>
    public string StyleId => "AuroraWaves";

    /// <inheritdoc/>
    public Task<string> GenerateAsync(
        ColorPalette palette,
        WeatherData weather,
        int width,
        int height,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("AuroraWaves: generating {W}×{H}", width, height);

        var rng = new Random(WeatherSeed.Build(weather));

        // Modulate visual parameters from meteorological data
        float windFactor = (float)Math.Clamp(weather.WindspeedKmh / 50.0, 0.4, 1.8);
        float humidityGlow = (float)Math.Clamp(weather.Humidity / 100.0, 0.3, 0.9);
        int layerCount = (int)Math.Clamp(5 + weather.WindspeedKmh / 20.0, 5, 8);

        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;

        // 1. Deep atmospheric background gradient
        var bgStartColor = palette.PrimaryColors[0];
        var bgEndColor = palette.PrimaryColors[^1];
        using (var bgShader = SKShader.CreateLinearGradient(
            new SKPoint(0, 0),
            new SKPoint(width * 0.5f, height),
            [bgStartColor, bgEndColor],
            SKShaderTileMode.Clamp))
        using (var bgPaint = new SKPaint { Shader = bgShader, IsAntialias = true, IsDither = true })
        {
            canvas.DrawRect(SKRect.Create(width, height), bgPaint);
        }

        // Combine primary and secondary colors for wave diversity
        var allColors = palette.PrimaryColors.Concat(palette.SecondaryColors).ToArray();

        // 2. Render undulating ribbon layers
        float baseYStep = height / (float)(layerCount + 2);
        for (int i = 0; i < layerCount; i++)
        {
            float baseY = baseYStep * (i + 1.5f);

            var primaryWaveColor = allColors[(i + rng.Next(allColors.Length)) % allColors.Length];
            var accentWaveColor = allColors[rng.Next(allColors.Length)];

            float amplitude = (height * 0.12f + rng.NextSingle() * (height * 0.14f)) * windFactor;
            float freq1 = 1.4f + rng.NextSingle() * 1.4f * windFactor;
            float freq2 = 2.4f + rng.NextSingle() * 1.8f;
            float freq3 = 0.8f + rng.NextSingle() * 0.8f;
            float phase = rng.NextSingle() * MathF.PI * 2f;

            // Generate high-density sample points for smooth curve construction
            int pointCount = 200;
            var points = new SKPoint[pointCount + 3];
            float dx = (width + 200f) / (float)(pointCount - 1);

            for (int p = 0; p < pointCount; p++)
            {
                float x = -100f + p * dx;
                float y = ComputeY(x, baseY, amplitude, freq1, freq2, freq3, phase, width);
                points[p + 1] = new SKPoint(x, y);
            }
            // Clamped ghost endpoints for smooth Catmull-Rom spline boundaries
            points[0] = new SKPoint(points[1].X - dx, points[1].Y);
            points[^2] = new SKPoint(points[^3].X + dx, points[^3].Y);
            points[^1] = new SKPoint(points[^2].X + dx, points[^2].Y);

            // Construct perfectly smooth C¹-continuous cubic Bézier spline path
            using var fillPath = new SKPath();
            fillPath.MoveTo(0, height);
            fillPath.LineTo(points[1].X, points[1].Y);
            AddSmoothSpline(fillPath, points);
            fillPath.LineTo(width, height);
            fillPath.Close();

            // Gradient fill from crest to bottom
            byte alpha = (byte)Math.Clamp(140 + i * 15, 120, 220);
            var colorTop = primaryWaveColor.WithAlpha(alpha);
            var colorBottom = accentWaveColor.WithAlpha((byte)(alpha * 0.35f));

            using var waveShader = SKShader.CreateLinearGradient(
                new SKPoint(0, baseY - amplitude),
                new SKPoint(width * 0.8f, height),
                [colorTop, colorBottom],
                SKShaderTileMode.Clamp);

            using var wavePaint = new SKPaint
            {
                Shader = waveShader,
                IsAntialias = true,
                IsDither = true,
                BlendMode = (i % 2 == 0) ? SKBlendMode.SrcOver : SKBlendMode.Screen
            };

            // Soft blur for humidity diffusion on upper layers
            if (i > 1 && humidityGlow > 0.4f)
            {
                float blurRadius = 8f * humidityGlow;
                wavePaint.ImageFilter = SKImageFilter.CreateBlur(blurRadius, blurRadius);
            }

            canvas.DrawPath(fillPath, wavePaint);

            // Luminous crest stroke along the smooth spline
            using var crestPaint = new SKPaint
            {
                Color = accentWaveColor.WithAlpha((byte)(alpha * 0.85f)),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2.2f + rng.NextSingle() * 2.0f,
                IsAntialias = true,
                BlendMode = SKBlendMode.Plus
            };

            using var crestPath = new SKPath();
            crestPath.MoveTo(points[1]);
            AddSmoothSpline(crestPath, points);
            canvas.DrawPath(crestPath, crestPaint);
        }

        // 3. Ambient lighting vignette
        float darken = (float)Math.Clamp(weather.CloudCoverPercent / 2000.0, 0.0, 0.05);
        if (darken > 0.005f)
        {
            using var overlay = new SKPaint
            {
                IsAntialias = true,
                Color = new SKColor(0, 0, 0, (byte)(darken * 255))
            };
            canvas.DrawRect(SKRect.Create(width, height), overlay);
        }

        SaveImage(surface, outputPath);
        return Task.FromResult(outputPath);
    }

    private static float ComputeY(float x, float baseY, float amplitude, float freq1, float freq2, float freq3, float phase, float width)
    {
        float normalizedX = x / width;
        float wave1 = MathF.Sin(normalizedX * freq1 * MathF.PI * 2f + phase);
        float wave2 = MathF.Cos(normalizedX * freq2 * MathF.PI * 2f + phase * 1.4f) * 0.35f;
        float wave3 = MathF.Sin(normalizedX * freq3 * MathF.PI * 2f + phase * 0.7f) * 0.20f;
        return baseY + (wave1 + wave2 + wave3) * amplitude;
    }

    private static void AddSmoothSpline(SKPath path, SKPoint[] pts)
    {
        // Catmull-Rom to Cubic Bézier spline conversion (pts length is count + 3)
        for (int i = 1; i < pts.Length - 2; i++)
        {
            var p0 = pts[i - 1];
            var p1 = pts[i];
            var p2 = pts[i + 1];
            var p3 = pts[i + 2];

            // Tension = 0.5 (standard Catmull-Rom)
            var cp1 = new SKPoint(
                p1.X + (p2.X - p0.X) / 6f,
                p1.Y + (p2.Y - p0.Y) / 6f);

            var cp2 = new SKPoint(
                p2.X - (p3.X - p1.X) / 6f,
                p2.Y - (p3.Y - p1.Y) / 6f);

            path.CubicTo(cp1, cp2, p2);
        }
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
