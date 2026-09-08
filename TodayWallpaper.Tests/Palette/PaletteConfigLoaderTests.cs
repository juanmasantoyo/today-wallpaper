using Microsoft.Extensions.Logging;
using Moq;
using SkiaSharp;
using TodayWallpaper.Core.Palette;
using TodayWallpaper.Core.Weather;
using Xunit;

namespace TodayWallpaper.Tests.Palette;

public class PaletteConfigLoaderTests
{
    private readonly Mock<ILogger<PaletteConfigLoader>> _loggerMock = new();

    [Fact]
    public void Load_ReturnsNonNullConfigWithAllConditions()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"tw_palettes_{Guid.NewGuid():N}.json");
        try
        {
            var loader = new PaletteConfigLoader(_loggerMock.Object, tempFile);
            var config = loader.Load();

            Assert.NotNull(config);
            Assert.NotNull(config.Empathic);
            Assert.NotNull(config.Contrast);

            foreach (var condition in Enum.GetValues<WeatherCondition>())
            {
                Assert.True(config.Empathic.ContainsKey(condition), $"Empathic missing condition {condition}");
                Assert.True(config.Contrast.ContainsKey(condition), $"Contrast missing condition {condition}");

                var empathicEntries = config.Empathic[condition];
                Assert.NotEmpty(empathicEntries);
                Assert.False(string.IsNullOrWhiteSpace(empathicEntries[0].Name));
                Assert.NotEmpty(empathicEntries[0].Primary);
                Assert.NotEmpty(empathicEntries[0].Secondary);

                var contrastEntries = config.Contrast[condition];
                Assert.NotEmpty(contrastEntries);
                Assert.False(string.IsNullOrWhiteSpace(contrastEntries[0].Name));
                Assert.NotEmpty(contrastEntries[0].Primary);
                Assert.NotEmpty(contrastEntries[0].Secondary);
            }
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void Load_AllDefaultPaletteColorsMeetLuminanceConstraint()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"tw_palettes_{Guid.NewGuid():N}.json");
        try
        {
            var loader = new PaletteConfigLoader(_loggerMock.Object, tempFile);
            var config = loader.Load();

            foreach (var entries in config.Empathic.Values)
            {
                foreach (var entry in entries)
                {
                    foreach (var color in entry.Primary)
                        Assert.True(LuminanceValidator.IsAcceptable(color), $"Primary color {color} exceeds max lightness");
                    foreach (var color in entry.Secondary)
                        Assert.True(LuminanceValidator.IsAcceptable(color), $"Secondary color {color} exceeds max lightness");
                }
            }

            foreach (var entries in config.Contrast.Values)
            {
                foreach (var entry in entries)
                {
                    foreach (var color in entry.Primary)
                        Assert.True(LuminanceValidator.IsAcceptable(color), $"Primary color {color} exceeds max lightness");
                    foreach (var color in entry.Secondary)
                        Assert.True(LuminanceValidator.IsAcceptable(color), $"Secondary color {color} exceeds max lightness");
                }
            }
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void Load_WhenConfigFileIsCorrupted_FallsBackToDefaultPalettes()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"tw_palettes_{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(tempFile, "{ broken json syntax");

            var loader = new PaletteConfigLoader(_loggerMock.Object, tempFile);
            var config = loader.Load();

            Assert.NotNull(config);
            Assert.NotNull(config.Empathic);
            Assert.NotEmpty(config.Empathic[WeatherCondition.Sunny][0].Primary);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void Load_PreservesCustomColorsEvenIfLightnessIsOutOfRecommendedRange()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"tw_palettes_{Guid.NewGuid():N}.json");
        try
        {
            // Custom bright color #FFFFFF and dark color #050505 should NOT be rejected (recommendation policy)
            var customJson = """
            {
                "empathic": {
                    "Sunny": [
                        {
                            "name": "Bright Summer",
                            "isEnabled": true,
                            "primary": ["#FFFFFF", "#E0E0E0"],
                            "secondary": ["#101010"]
                        }
                    ]
                },
                "contrast": {}
            }
            """;

            File.WriteAllText(tempFile, customJson);

            var loader = new PaletteConfigLoader(_loggerMock.Object, tempFile);
            var config = loader.Load();

            Assert.NotNull(config);
            var sunnyPalette = config.Empathic[WeatherCondition.Sunny][0];
            Assert.Equal("Bright Summer", sunnyPalette.Name);
            Assert.True(sunnyPalette.IsEnabled);
            Assert.Equal(2, sunnyPalette.Primary.Count);
            Assert.Equal(new SKColor(0xFF, 0xFF, 0xFF), sunnyPalette.Primary[0]);
            Assert.Equal(new SKColor(0x10, 0x10, 0x10), sunnyPalette.Secondary[0]);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void Load_WhenConfigContainsInvalidHexSyntax_FallsBackToDefaults()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"tw_palettes_{Guid.NewGuid():N}.json");
        try
        {
            var customJson = """
            {
                "empathic": {
                    "Sunny": [
                        {
                            "name": "Broken Hex",
                            "isEnabled": true,
                            "primary": ["#ZZZZZZ"],
                            "secondary": ["#YYYYYY"]
                        }
                    ]
                },
                "contrast": {}
            }
            """;

            File.WriteAllText(tempFile, customJson);

            var loader = new PaletteConfigLoader(_loggerMock.Object, tempFile);
            var config = loader.Load();

            Assert.NotNull(config);
            Assert.NotEmpty(config.Empathic[WeatherCondition.Sunny][0].Primary);
            Assert.True(LuminanceValidator.IsAcceptable(config.Empathic[WeatherCondition.Sunny][0].Primary[0]));
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void Save_PersistsModifiedPaletteConfigAndCanBeReloaded()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"tw_palettes_{Guid.NewGuid():N}.json");
        try
        {
            var loader = new PaletteConfigLoader(_loggerMock.Object, tempFile);
            var config = loader.Load();

            // Modify sunny palette with 2 custom palettes
            var p1 = new PaletteEntry("Forest", true, [new SKColor(0x20, 0x60, 0x40)], [new SKColor(0x40, 0x40, 0x50)]);
            var p2 = new PaletteEntry("Ocean", false, [new SKColor(0x20, 0x40, 0x60)], [new SKColor(0x30, 0x30, 0x40)]);
            config.Empathic[WeatherCondition.Sunny] = [p1, p2];

            loader.Save(config);

            // Create a new loader instance to load from the same file
            var reloader = new PaletteConfigLoader(_loggerMock.Object, tempFile);
            var reloaded = reloader.Load();

            var sunnyPalettes = reloaded.Empathic[WeatherCondition.Sunny];
            Assert.Equal(2, sunnyPalettes.Count);

            Assert.Equal("Forest", sunnyPalettes[0].Name);
            Assert.True(sunnyPalettes[0].IsEnabled);
            Assert.Equal(new SKColor(0x20, 0x60, 0x40), sunnyPalettes[0].Primary[0]);

            Assert.Equal("Ocean", sunnyPalettes[1].Name);
            Assert.False(sunnyPalettes[1].IsEnabled);
            Assert.Equal(new SKColor(0x20, 0x40, 0x60), sunnyPalettes[1].Primary[0]);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void ResetToDefaults_RestoresDefaultPalettes()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"tw_palettes_{Guid.NewGuid():N}.json");
        try
        {
            var loader = new PaletteConfigLoader(_loggerMock.Object, tempFile);
            var config = loader.Load();

            // Save modified config
            config.Empathic[WeatherCondition.Sunny] = [new PaletteEntry("Custom", true, [new SKColor(0x20, 0x50, 0x30)], [new SKColor(0x30, 0x30, 0x40)])];
            loader.Save(config);

            // Now reset
            loader.ResetToDefaults();
            var defaults = loader.Load();

            var hardcodedDefault = loader.GetDefaultConfig();
            Assert.Equal(hardcodedDefault.Empathic[WeatherCondition.Sunny][0].Primary.Count, defaults.Empathic[WeatherCondition.Sunny][0].Primary.Count);
            Assert.Equal(hardcodedDefault.Empathic[WeatherCondition.Sunny][0].Primary[0], defaults.Empathic[WeatherCondition.Sunny][0].Primary[0]);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}

