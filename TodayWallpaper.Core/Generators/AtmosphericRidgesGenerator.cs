using Microsoft.Extensions.Logging;
using SkiaSharp;
using TodayWallpaper.Core.Palette;
using TodayWallpaper.Core.Weather;

namespace TodayWallpaper.Core.Generators;

/// <summary>
/// Generates a minimalist landscape wallpaper with layered mountain ridges and atmospheric depth fog.
/// Visibility and humidity control fog density and distance falloff; wind speed modulates ridge roughness.
/// </summary>
public sealed class AtmosphericRidgesGenerator(ILogger<AtmosphericRidgesGenerator> logger) : IWallpaperGenerator
{
    /// <inheritdoc/>
    public string StyleId => "AtmosphericRidges";

    /// <inheritdoc/>
    public Task<string> GenerateAsync(
        ColorPalette palette,
        WeatherData weather,
        int width,
        int height,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("AtmosphericRidges: generating {W}×{H}", width, height);

        var rng = new Random(WeatherSeed.Build(weather));

        // Weather parameter mappings
        int ridgeCount = (int)Math.Clamp(4 + (weather.WindspeedKmh / 25.0), 4, 6);
        float roughness = (float)Math.Clamp(0.6 + (weather.WindspeedKmh / 60.0), 0.5, 1.4);
        float fogDensity = (float)Math.Clamp((weather.Humidity / 100.0) * 0.7f + (1.0 - Math.Min(weather.VisibilityKm, 20.0) / 20.0) * 0.3f, 0.2, 0.85);

        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;

        var primarySky = palette.PrimaryColors[0];
        var secondarySky = palette.PrimaryColors[^1];
        var accentGlow = palette.SecondaryColors[rng.Next(palette.SecondaryColors.Count)];

        // 1. Sky Gradient Background
        using (var skyShader = SKShader.CreateLinearGradient(
            new SKPoint(0, 0),
            new SKPoint(0, height * 0.85f),
            [primarySky, secondarySky],
            SKShaderTileMode.Clamp))
        using (var skyPaint = new SKPaint { Shader = skyShader, IsAntialias = true, IsDither = true })
        {
            canvas.DrawRect(SKRect.Create(width, height), skyPaint);
        }

        // 2. Ambient celestial / atmospheric glow on horizon
        float glowCenterX = width * (0.3f + rng.NextSingle() * 0.4f);
        float glowCenterY = height * 0.42f;
        float glowRadius = Math.Max(width, height) * 0.45f;

        using (var glowShader = SKShader.CreateRadialGradient(
            new SKPoint(glowCenterX, glowCenterY),
            glowRadius,
            [accentGlow.WithAlpha((byte)(90 * (1f - fogDensity * 0.4f))), accentGlow.WithAlpha(0)],
            SKShaderTileMode.Clamp))
        using (var glowPaint = new SKPaint { Shader = glowShader, IsAntialias = true, BlendMode = SKBlendMode.Screen })
        {
            canvas.DrawCircle(glowCenterX, glowCenterY, glowRadius, glowPaint);
        }

        // 3. Render Mountain Ridge Layers (Back to Front)
        var foregroundColor = palette.PrimaryColors[0];
        var ridgeColors = palette.SecondaryColors.Concat(palette.PrimaryColors).ToArray();

        float baseHeightStart = height * 0.38f;
        float baseHeightStep = (height * 0.52f) / ridgeCount;

        int samples = 400;
        float dx = width / (float)(samples - 1);

        for (int r = 0; r < ridgeCount; r++)
        {
            float depthT = (float)r / (ridgeCount - 1); // 0 = furthest background, 1 = closest foreground
            float horizonY = baseHeightStart + r * baseHeightStep;

            float layerAmp = (height * 0.08f + rng.NextSingle() * (height * 0.06f)) * (1.2f - depthT * 0.3f);
            float freq1 = (2.0f + rng.NextSingle() * 2.0f) * roughness;
            float freq2 = (5.0f + rng.NextSingle() * 4.0f) * roughness;
            float phase = rng.NextSingle() * 100f;

            using var ridgePath = new SKPath();
            ridgePath.MoveTo(0, height);
            ridgePath.LineTo(0, SampleRidgeY(0, horizonY, layerAmp, freq1, freq2, phase, width));

            for (int s = 1; s < samples; s++)
            {
                float x = s * dx;
                float y = SampleRidgeY(x, horizonY, layerAmp, freq1, freq2, phase, width);
                ridgePath.LineTo(x, y);
            }

            ridgePath.LineTo(width, height);
            ridgePath.Close();

            // Blend ridge color towards sky with fog for depth
            var baseRidgeColor = ridgeColors[(r + rng.Next(ridgeColors.Length)) % ridgeColors.Length];
            var ridgeColor = BlendWithFog(baseRidgeColor, secondarySky, 1f - depthT, fogDensity);

            // Layer fill gradient from peak to bottom
            var bottomColor = AdjustLightness(ridgeColor, 0.75f);
            using var ridgeShader = SKShader.CreateLinearGradient(
                new SKPoint(0, horizonY - layerAmp),
                new SKPoint(0, height),
                [ridgeColor, bottomColor],
                SKShaderTileMode.Clamp);

            using var ridgePaint = new SKPaint
            {
                Shader = ridgeShader,
                IsAntialias = true,
                IsDither = true
            };

            canvas.DrawPath(ridgePath, ridgePaint);

            // Atmospheric haze mist layer between ridges
            if (r < ridgeCount - 1 && fogDensity > 0.25f)
            {
                byte mistAlpha = (byte)(fogDensity * (1f - depthT) * 75);
                using var mistShader = SKShader.CreateLinearGradient(
                    new SKPoint(0, horizonY - layerAmp * 0.5f),
                    new SKPoint(0, horizonY + baseHeightStep),
                    [secondarySky.WithAlpha(mistAlpha), secondarySky.WithAlpha(0)],
                    SKShaderTileMode.Clamp);

                using var mistPaint = new SKPaint
                {
                    Shader = mistShader,
                    IsAntialias = true,
                    BlendMode = SKBlendMode.SrcOver
                };

                canvas.DrawRect(SKRect.Create(0, horizonY - layerAmp, width, height - (horizonY - layerAmp)), mistPaint);
            }
        }

        // 4. Subtle ambient lighting vignette
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

    private static float SampleRidgeY(float x, float baseY, float amp, float freq1, float freq2, float phase, float width)
    {
        float nx = (x / width);
        float h1 = MathF.Sin(nx * freq1 * MathF.PI * 2f + phase);
        float h2 = MathF.Sin(nx * freq2 * MathF.PI * 2f + phase * 2.3f) * 0.35f;
        float h3 = NoiseHelper.Noise(nx * freq1 * 4f + phase, phase) * 0.25f;
        return baseY + (h1 + h2 + h3) * amp;
    }

    private static SKColor BlendWithFog(SKColor color, SKColor fogColor, float distance, float density)
    {
        float fogFactor = Math.Clamp(distance * density, 0f, 0.85f);
        byte r = (byte)(color.Red * (1f - fogFactor) + fogColor.Red * fogFactor);
        byte g = (byte)(color.Green * (1f - fogFactor) + fogColor.Green * fogFactor);
        byte b = (byte)(color.Blue * (1f - fogFactor) + fogColor.Blue * fogFactor);

        var blended = new SKColor(r, g, b, 255);
        blended.ToHsl(out float h, out float s, out float l);
        float clampedL = Math.Clamp(l, 14f, 55f);
        return SKColor.FromHsl(h, s, clampedL, 255);
    }

    private static SKColor AdjustLightness(SKColor color, float factor)
    {
        color.ToHsl(out float h, out float s, out float l);
        float newL = Math.Clamp(l * factor, 12f, 55f);
        return SKColor.FromHsl(h, s, newL, color.Alpha);
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
