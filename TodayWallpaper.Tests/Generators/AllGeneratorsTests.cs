using Microsoft.Extensions.Logging;
using Moq;
using SkiaSharp;
using TodayWallpaper.Core.Generators;
using TodayWallpaper.Core.Palette;
using TodayWallpaper.Core.Weather;
using Xunit;

namespace TodayWallpaper.Tests.Generators;

public class AllGeneratorsTests
{
    private static WeatherData CreateSampleWeather(WeatherCondition condition = WeatherCondition.Rainy) =>
        new(
            Condition: condition,
            WmoCode: 61,
            TemperatureCelsius: 16.5,
            ApparentTemperatureCelsius: 15.0,
            WindspeedKmh: 25.0,
            Humidity: 80,
            PrecipitationMm: 12.0,
            CloudCoverPercent: 90,
            VisibilityKm: 6.0,
            DewpointCelsius: 13.0,
            LocationName: "Galicia");

    private static ColorPalette CreateSamplePalette() =>
        new(
            new[] { new SKColor(15, 25, 45), new SKColor(25, 40, 70), new SKColor(35, 55, 90) },
            new[] { new SKColor(50, 80, 110), new SKColor(70, 100, 130) },
            0.4f);

    [Theory]
    [InlineData("BlurBlobs")]
    [InlineData("LowPoly")]
    [InlineData("PerlinFlow")]
    [InlineData("RadialGradient")]
    [InlineData("GeometricShapes")]
    public async Task Generator_ProducesValidImage(string styleId)
    {
        IWallpaperGenerator generator = styleId switch
        {
            "BlurBlobs" => new BlurBlobsGenerator(new Mock<ILogger<BlurBlobsGenerator>>().Object),
            "LowPoly" => new LowPolyGenerator(new Mock<ILogger<LowPolyGenerator>>().Object),
            "PerlinFlow" => new PerlinFlowGenerator(new Mock<ILogger<PerlinFlowGenerator>>().Object),
            "RadialGradient" => new RadialGradientGenerator(new Mock<ILogger<RadialGradientGenerator>>().Object),
            "GeometricShapes" => new GeometricShapesGenerator(new Mock<ILogger<GeometricShapesGenerator>>().Object),
            _ => throw new ArgumentOutOfRangeException(nameof(styleId))
        };

        Assert.Equal(styleId, generator.StyleId);

        var tempFile = Path.Combine(Path.GetTempPath(), $"test_gen_{styleId}_{Guid.NewGuid():N}.jpg");
        try
        {
            var weather = CreateSampleWeather();
            var palette = CreateSamplePalette();

            var result = await generator.GenerateAsync(palette, weather, 160, 90, tempFile);

            Assert.Equal(tempFile, result);
            Assert.True(File.Exists(tempFile));

            using var bmp = SKBitmap.Decode(tempFile);
            Assert.NotNull(bmp);
            Assert.Equal(160, bmp.Width);
            Assert.Equal(90, bmp.Height);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LowPoly_GeneratesDiverseTrianglesAndColors()
    {
        var generator = new LowPolyGenerator(new Mock<ILogger<LowPolyGenerator>>().Object);
        var weather = CreateSampleWeather();
        var palette = CreateSamplePalette();

        var tempFile = Path.Combine(Path.GetTempPath(), $"test_lowpoly_diversity_{Guid.NewGuid():N}.jpg");
        try
        {
            await generator.GenerateAsync(palette, weather, 320, 180, tempFile);
            Assert.True(File.Exists(tempFile));

            using var bmp = SKBitmap.Decode(tempFile);
            Assert.NotNull(bmp);
            Assert.Equal(320, bmp.Width);
            Assert.Equal(180, bmp.Height);

            // Sample pixels across a grid to ensure multiple distinct colors exist (not a flat plane)
            var uniqueColors = new HashSet<int>();
            for (int y = 10; y < bmp.Height - 10; y += 15)
            {
                for (int x = 10; x < bmp.Width - 10; x += 15)
                {
                    var c = bmp.GetPixel(x, y);
                    // Quantize slightly (high 5 bits of each channel) to ignore JPEG compression artifacts
                    int quantized = ((c.Red >> 3) << 10) | ((c.Green >> 3) << 5) | (c.Blue >> 3);
                    uniqueColors.Add(quantized);
                }
            }

            // A flat color has only 1-2 quantized values due to minor JPEG artifacts.
            // A genuine low-poly faceted mesh has dozens of distinct facet shades.
            Assert.True(uniqueColors.Count >= 10,
                $"LowPoly should generate a diverse faceted mesh with many distinct colors, but got only {uniqueColors.Count} unique colors (likely flat).");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void Factory_ResolvesStylesCorrectly()
    {
        var g1 = new BlurBlobsGenerator(new Mock<ILogger<BlurBlobsGenerator>>().Object);
        var g2 = new LowPolyGenerator(new Mock<ILogger<LowPolyGenerator>>().Object);
        var g3 = new RadialGradientGenerator(new Mock<ILogger<RadialGradientGenerator>>().Object);

        var factory = new WallpaperGeneratorFactory(
            new IWallpaperGenerator[] { g1, g2, g3 },
            new Mock<ILogger<WallpaperGeneratorFactory>>().Object);

        Assert.Equal(3, factory.StyleIds.Count);
        Assert.Contains("BlurBlobs", factory.StyleIds);
        Assert.Contains("LowPoly", factory.StyleIds);
        Assert.Contains("RadialGradient", factory.StyleIds);

        // Exact match
        var resolved = factory.Resolve("LowPoly");
        Assert.Equal("LowPoly", resolved.StyleId);

        // Multi-selection resolution (exact 1 in list)
        var singleSelected = factory.Resolve(new[] { "LowPoly" });
        Assert.Equal("LowPoly", singleSelected.StyleId);

        // Multi-selection resolution (subset)
        var weather = CreateSampleWeather();
        var subsetResolved = factory.Resolve(new[] { "BlurBlobs", "RadialGradient" }, weather);
        Assert.NotNull(subsetResolved);
        Assert.Contains(subsetResolved.StyleId, new[] { "BlurBlobs", "RadialGradient" });

        // Random resolution
        var randomResolved = factory.Resolve("Random", weather);
        Assert.NotNull(randomResolved);

        // Unknown resolution fallback
        var fallbackResolved = factory.Resolve("NonExistentStyle", weather);
        Assert.NotNull(fallbackResolved);

        // Empty selection fallback
        var emptyResolved = factory.Resolve(Array.Empty<string>(), weather);
        Assert.NotNull(emptyResolved);
    }

    [Fact]
    public void GeneratorStyles_HaveLocalizedDisplayNames()
    {
        var styleIds = new[] { "BlurBlobs", "LowPoly", "PerlinFlow", "RadialGradient", "GeometricShapes" };
        foreach (var styleId in styleIds)
        {
            var localizedName = TodayWallpaper.Core.Localization.Strings.GetGeneratorStyleName(styleId);
            Assert.False(string.IsNullOrWhiteSpace(localizedName));
            Assert.NotEqual(styleId, localizedName); // English and Spanish friendly names differ from raw ID
        }
    }

    [Theory]
    [InlineData("BlurBlobs")]
    [InlineData("LowPoly")]
    [InlineData("PerlinFlow")]
    [InlineData("RadialGradient")]
    [InlineData("GeometricShapes")]
    public async Task StormyWallpaper_HasHealthyLuminanceAndIsNotMuddyBlack(string styleId)
    {
        IWallpaperGenerator generator = styleId switch
        {
            "BlurBlobs" => new BlurBlobsGenerator(new Mock<ILogger<BlurBlobsGenerator>>().Object),
            "LowPoly" => new LowPolyGenerator(new Mock<ILogger<LowPolyGenerator>>().Object),
            "PerlinFlow" => new PerlinFlowGenerator(new Mock<ILogger<PerlinFlowGenerator>>().Object),
            "RadialGradient" => new RadialGradientGenerator(new Mock<ILogger<RadialGradientGenerator>>().Object),
            "GeometricShapes" => new GeometricShapesGenerator(new Mock<ILogger<GeometricShapesGenerator>>().Object),
            _ => throw new ArgumentOutOfRangeException(nameof(styleId))
        };

        var mapper = new PaletteMapper(new PaletteConfigLoader(new Mock<ILogger<PaletteConfigLoader>>().Object));
        var stormyWeather = new WeatherData(
            Condition: WeatherCondition.Stormy,
            WmoCode: 95,
            TemperatureCelsius: 9.0,
            ApparentTemperatureCelsius: 5.0,
            WindspeedKmh: 70.0,
            Humidity: 90.0,
            PrecipitationMm: 20.0,
            CloudCoverPercent: 95.0,
            VisibilityKm: 3.0,
            DewpointCelsius: 8.0,
            LocationName: "Storm City");

        var palette = mapper.Map(WeatherCondition.Stormy, PaletteMode.Empathic, stormyWeather);

        var tempFile = Path.Combine(Path.GetTempPath(), $"stormy_lum_test_{styleId}_{Guid.NewGuid():N}.jpg");
        try
        {
            await generator.GenerateAsync(palette, stormyWeather, 160, 90, tempFile);
            using var bmp = SKBitmap.Decode(tempFile);
            Assert.NotNull(bmp);

            float totalL = 0;
            int step = 4;
            int count = 0;
            for (int y = 0; y < bmp.Height; y += step)
            {
                for (int x = 0; x < bmp.Width; x += step)
                {
                    var color = bmp.GetPixel(x, y);
                    color.ToHsl(out _, out _, out float l);
                    totalL += l;
                    count++;
                }
            }
            float avgL = totalL / count;

            Assert.True(avgL >= 18f, $"Style {styleId} in Stormy had average lightness {avgL}%, which is too dark!");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Theory]
    [InlineData(WeatherCondition.Sunny)]
    [InlineData(WeatherCondition.Rainy)]
    public async Task SunnyAndRainy_LowPoly_PreservesColorDiversityAndHealthyLuminance(WeatherCondition condition)
    {
        var generator = new LowPolyGenerator(new Mock<ILogger<LowPolyGenerator>>().Object);
        var mapper = new PaletteMapper(new PaletteConfigLoader(new Mock<ILogger<PaletteConfigLoader>>().Object));

        var weather = condition == WeatherCondition.Sunny
            ? new WeatherData(WeatherCondition.Sunny, 0, 26, 27, 15, 45, 0, 10, 30, 12, "Sunny City")
            : new WeatherData(WeatherCondition.Rainy, 61, 14, 13, 20, 85, 5, 80, 12, 11, "Spring Rain City");

        var palette = mapper.Map(condition, PaletteMode.Empathic, weather);
        var tempFile = Path.Combine(Path.GetTempPath(), $"lowpoly_{condition}_{Guid.NewGuid():N}.jpg");

        try
        {
            await generator.GenerateAsync(palette, weather, 200, 120, tempFile);
            Assert.True(File.Exists(tempFile));

            using var bmp = SKBitmap.Decode(tempFile);
            Assert.NotNull(bmp);

            var hues = new List<float>();
            float sumL = 0;
            int count = 0;
            for (int y = 5; y < bmp.Height; y += 5)
            {
                for (int x = 5; x < bmp.Width; x += 5)
                {
                    var c = bmp.GetPixel(x, y);
                    c.ToHsl(out float h, out _, out float l);
                    hues.Add(h);
                    sumL += l;
                    count++;
                    Assert.True(l >= 16f, $"Pixel at ({x},{y}) had too dark lightness: {l}% (below JPEG artifact threshold)");
                }
            }

            Assert.True(sumL / count >= 20f, $"Average lightness for {condition} LowPoly was below 20%");

            // Verify that multiple hue sectors are present (not flat monochrome)
            float minH = hues.Min();
            float maxH = hues.Max();
            float hueSpread = maxH - minH;
            Assert.True(hueSpread > 20f, $"Hue spread across LowPoly {condition} was only {hueSpread}°, expected rich color variations.");
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}
