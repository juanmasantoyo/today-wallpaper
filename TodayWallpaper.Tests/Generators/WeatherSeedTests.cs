using TodayWallpaper.Core;
using TodayWallpaper.Core.Weather;
using Xunit;

namespace TodayWallpaper.Tests.Generators;

public class WeatherSeedTests
{
    private static WeatherData CreateData(
        WeatherCondition condition = WeatherCondition.Sunny,
        double temp = 20,
        double precip = 0,
        double wind = 10) =>
        new(
            Condition: condition,
            WmoCode: (int)condition,
            TemperatureCelsius: temp,
            ApparentTemperatureCelsius: temp,
            WindspeedKmh: wind,
            Humidity: 50,
            PrecipitationMm: precip,
            CloudCoverPercent: 10,
            VisibilityKm: 10,
            DewpointCelsius: 5,
            LocationName: "City");

    [Fact]
    public void Build_SameInputSameTimestamp_ReturnsIdenticalSeed()
    {
        var data = CreateData();
        var fixedTime = new DateTime(2026, 9, 5, 14, 0, 0);

        var seed1 = WeatherSeed.Build(data, fixedTime);
        var seed2 = WeatherSeed.Build(data, fixedTime);

        Assert.Equal(seed1, seed2);
    }

    [Fact]
    public void Build_DifferentHour_ProducesDifferentSeed()
    {
        var data = CreateData();
        var time1 = new DateTime(2026, 9, 5, 10, 0, 0);
        var time2 = new DateTime(2026, 9, 5, 11, 0, 0);

        var seed1 = WeatherSeed.Build(data, time1);
        var seed2 = WeatherSeed.Build(data, time2);

        Assert.NotEqual(seed1, seed2);
    }

    [Fact]
    public void Build_DifferentCondition_ProducesDifferentSeed()
    {
        var fixedTime = new DateTime(2026, 9, 5, 14, 0, 0);
        var data1 = CreateData(condition: WeatherCondition.Sunny);
        var data2 = CreateData(condition: WeatherCondition.Rainy);

        var seed1 = WeatherSeed.Build(data1, fixedTime);
        var seed2 = WeatherSeed.Build(data2, fixedTime);

        Assert.NotEqual(seed1, seed2);
    }

    [Fact]
    public void Build_DifferentTemperature_ProducesDifferentSeed()
    {
        var fixedTime = new DateTime(2026, 9, 5, 14, 0, 0);
        var data1 = CreateData(temp: 10);
        var data2 = CreateData(temp: 30);

        var seed1 = WeatherSeed.Build(data1, fixedTime);
        var seed2 = WeatherSeed.Build(data2, fixedTime);

        Assert.NotEqual(seed1, seed2);
    }
}
