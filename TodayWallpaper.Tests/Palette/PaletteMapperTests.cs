using Moq;
using SkiaSharp;
using TodayWallpaper.Core.Palette;
using TodayWallpaper.Core.Weather;
using Xunit;

namespace TodayWallpaper.Tests.Palette;

public class PaletteMapperTests
{
    private static WeatherData CreateWeather(WeatherCondition condition, double temp) =>
        new(
            Condition: condition,
            WmoCode: 0,
            TemperatureCelsius: temp,
            ApparentTemperatureCelsius: temp,
            WindspeedKmh: 10,
            Humidity: 50,
            PrecipitationMm: 0,
            CloudCoverPercent: 10,
            VisibilityKm: 15,
            DewpointCelsius: 5,
            LocationName: "City");

    [Theory]
    [InlineData(WeatherCondition.Sunny, PaletteMode.Empathic)]
    [InlineData(WeatherCondition.Sunny, PaletteMode.Contrast)]
    [InlineData(WeatherCondition.Cloudy, PaletteMode.Empathic)]
    [InlineData(WeatherCondition.Cloudy, PaletteMode.Contrast)]
    [InlineData(WeatherCondition.Rainy, PaletteMode.Empathic)]
    [InlineData(WeatherCondition.Rainy, PaletteMode.Contrast)]
    [InlineData(WeatherCondition.Snowy, PaletteMode.Empathic)]
    [InlineData(WeatherCondition.Snowy, PaletteMode.Contrast)]
    [InlineData(WeatherCondition.Foggy, PaletteMode.Empathic)]
    [InlineData(WeatherCondition.Foggy, PaletteMode.Contrast)]
    [InlineData(WeatherCondition.Stormy, PaletteMode.Empathic)]
    [InlineData(WeatherCondition.Stormy, PaletteMode.Contrast)]
    public void Map_ReturnsValidColorPalette(WeatherCondition condition, PaletteMode mode)
    {
        var configLoaderMock = new Mock<IPaletteConfigLoader>();
        configLoaderMock.Setup(l => l.Load()).Returns(DefaultPalettes.Config);

        var mapper = new PaletteMapper(configLoaderMock.Object);
        var weather = CreateWeather(condition, 20.0);

        var palette = mapper.Map(condition, mode, weather);

        Assert.NotNull(palette);
        Assert.NotEmpty(palette.PrimaryColors);
        Assert.NotEmpty(palette.SecondaryColors);
        Assert.InRange(palette.Temperature, 0.0f, 1.0f);

        foreach (var c in palette.PrimaryColors)
            Assert.True(LuminanceValidator.IsAcceptable(c));
        foreach (var c in palette.SecondaryColors)
            Assert.True(LuminanceValidator.IsAcceptable(c));
    }

    [Fact]
    public void Map_NormalizesTemperatureCorrectly()
    {
        var configLoaderMock = new Mock<IPaletteConfigLoader>();
        configLoaderMock.Setup(l => l.Load()).Returns(DefaultPalettes.Config);

        var mapper = new PaletteMapper(configLoaderMock.Object);

        var freezingWeather = CreateWeather(WeatherCondition.Snowy, -10.0);
        var pFreezing = mapper.Map(WeatherCondition.Snowy, PaletteMode.Empathic, freezingWeather);
        Assert.Equal(0.0f, pFreezing.Temperature);

        var hotWeather = CreateWeather(WeatherCondition.Sunny, 40.0);
        var pHot = mapper.Map(WeatherCondition.Sunny, PaletteMode.Empathic, hotWeather);
        Assert.Equal(1.0f, pHot.Temperature);

        var mildWeather = CreateWeather(WeatherCondition.Cloudy, 15.0);
        var pMild = mapper.Map(WeatherCondition.Cloudy, PaletteMode.Empathic, mildWeather);
        Assert.Equal(0.5f, pMild.Temperature, 2);
    }

    [Fact]
    public void Map_SelectsOnlyFromEnabledPalettes()
    {
        var disabledPalette = new PaletteEntry("Disabled Yellow", false, [new SKColor(200, 200, 0)], [new SKColor(180, 180, 0)]);
        var enabledPalette = new PaletteEntry("Enabled Blue", true, [new SKColor(20, 40, 80)], [new SKColor(10, 20, 50)]);

        var config = new PaletteConfig(
            new Dictionary<WeatherCondition, List<PaletteEntry>>
            {
                [WeatherCondition.Sunny] = [disabledPalette, enabledPalette]
            },
            new Dictionary<WeatherCondition, List<PaletteEntry>>
            {
                [WeatherCondition.Sunny] = [enabledPalette]
            });

        var configLoaderMock = new Mock<IPaletteConfigLoader>();
        configLoaderMock.Setup(l => l.Load()).Returns(config);

        var mapper = new PaletteMapper(configLoaderMock.Object);
        var weather = CreateWeather(WeatherCondition.Sunny, 25.0);

        var result = mapper.Map(WeatherCondition.Sunny, PaletteMode.Empathic, weather);

        // Since enabledPalette has blue hues (Red ~20), verify that the jittered color is around blue, not yellow (Red ~200)
        Assert.True(result.PrimaryColors[0].Red < 60, "Should have selected the enabled blue palette, not disabled yellow.");
    }

    [Fact]
    public void Map_WhenAllPalettesDisabled_FallsBackGracefully()
    {
        var disabledPalette = new PaletteEntry("Disabled Ocean", false, [new SKColor(20, 50, 70)], [new SKColor(10, 30, 40)]);

        var config = new PaletteConfig(
            new Dictionary<WeatherCondition, List<PaletteEntry>>
            {
                [WeatherCondition.Sunny] = [disabledPalette]
            },
            new Dictionary<WeatherCondition, List<PaletteEntry>>
            {
                [WeatherCondition.Sunny] = [disabledPalette]
            });

        var configLoaderMock = new Mock<IPaletteConfigLoader>();
        configLoaderMock.Setup(l => l.Load()).Returns(config);
        configLoaderMock.Setup(l => l.GetDefaultConfig()).Returns(DefaultPalettes.Config);

        var mapper = new PaletteMapper(configLoaderMock.Object);
        var weather = CreateWeather(WeatherCondition.Sunny, 20.0);

        var result = mapper.Map(WeatherCondition.Sunny, PaletteMode.Empathic, weather);

        Assert.NotNull(result);
        Assert.NotEmpty(result.PrimaryColors);
    }
}
