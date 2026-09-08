using SkiaSharp;
using TodayWallpaper.Core.Palette;
using TodayWallpaper.Core.Weather;
using Xunit;

namespace TodayWallpaper.Tests.Palette;

public class ColorJitterTests
{
    private static WeatherData CreateSampleWeather(WeatherCondition condition = WeatherCondition.Sunny, double windspeed = 10) =>
        new(
            Condition: condition,
            WmoCode: 0,
            TemperatureCelsius: 20,
            ApparentTemperatureCelsius: 20,
            WindspeedKmh: windspeed,
            Humidity: 50,
            PrecipitationMm: 0,
            CloudCoverPercent: 20,
            VisibilityKm: 10,
            DewpointCelsius: 10,
            LocationName: "Test");

    [Fact]
    public void Apply_AlwaysKeepsLightnessWithinAcceptableRange()
    {
        var baseColor = new SKColor(80, 120, 160);
        var weather = CreateSampleWeather();

        for (int seed = 0; seed < 100; seed++)
        {
            var rng = new Random(seed);
            var jittered = ColorJitter.Apply(baseColor, rng, weather);

            jittered.ToHsl(out _, out _, out float l);
            Assert.True(l <= LuminanceValidator.MaxLightness + 0.01f, $"Lightness {l} exceeded {LuminanceValidator.MaxLightness}% with seed {seed}");
            Assert.True(l >= LuminanceValidator.MinLightness - 0.01f, $"Lightness {l} fell below {LuminanceValidator.MinLightness}% with seed {seed}");
            Assert.True(LuminanceValidator.IsAcceptable(jittered));
        }
    }

    [Fact]
    public void Apply_DeterministicWithSameSeed()
    {
        var baseColor = new SKColor(70, 90, 140);
        var weather = CreateSampleWeather();

        var color1 = ColorJitter.Apply(baseColor, new Random(42), weather);
        var color2 = ColorJitter.Apply(baseColor, new Random(42), weather);

        Assert.Equal(color1, color2);
    }
}
