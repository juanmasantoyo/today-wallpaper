using TodayWallpaper.Core.Weather;

namespace TodayWallpaper.Sandbox;

/// <summary>Realistic weather parameter presets for each <see cref="WeatherCondition"/>.</summary>
public static class WeatherPreset
{
    /// <summary>Returns a realistic <see cref="WeatherData"/> snapshot for the given condition.</summary>
    public static WeatherData For(WeatherCondition condition) => condition switch
    {
        WeatherCondition.Sunny  => Make(condition, 0,  28.0, 31.0, 12.0, 35.0, 0.0,   5.0,  40.0, 14.0),
        WeatherCondition.Cloudy => Make(condition, 2,  16.0, 15.0, 20.0, 65.0, 0.0,  55.0,  25.0, 10.0),
        WeatherCondition.Rainy  => Make(condition, 61, 12.0, 10.0, 35.0, 85.0, 8.0,  80.0,  10.0,  9.0),
        WeatherCondition.Stormy => Make(condition, 95,  9.0,  5.0, 70.0, 90.0,20.0,  95.0,   3.0,  8.0),
        WeatherCondition.Snowy  => Make(condition, 71, -3.0, -6.0, 18.0, 80.0, 3.0,  70.0,  15.0, -5.0),
        WeatherCondition.Foggy  => Make(condition, 45, 11.0, 10.0,  4.0, 95.0, 0.0, 100.0,   0.5,  10.0),
        _                       => Make(WeatherCondition.Cloudy, 2, 16.0, 15.0, 20.0, 65.0, 0.0, 55.0, 25.0, 10.0)
    };

    private static WeatherData Make(
        WeatherCondition condition, int wmo,
        double temp, double apparent, double wind, double humidity,
        double precip, double cloud, double visibility, double dew) =>
        new(condition, wmo, temp, apparent, wind, humidity, precip, cloud, visibility, dew, "Sandbox");
}
