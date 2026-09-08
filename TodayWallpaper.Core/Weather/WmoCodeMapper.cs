namespace TodayWallpaper.Core.Weather;

/// <summary>Maps WMO weather interpretation codes to coarse <see cref="WeatherCondition"/> buckets.</summary>
public static class WmoCodeMapper
{
    /// <summary>
    /// Returns the <see cref="WeatherCondition"/> that best describes the given WMO code.
    /// Unknown codes default to <see cref="WeatherCondition.Cloudy"/>.
    /// </summary>
    public static WeatherCondition Map(int wmoCode) => wmoCode switch
    {
        0                        => WeatherCondition.Sunny,
        1 or 2 or 3              => WeatherCondition.Cloudy,
        45 or 48                 => WeatherCondition.Foggy,
        >= 51 and <= 67          => WeatherCondition.Rainy,
        >= 71 and <= 77          => WeatherCondition.Snowy,
        80 or 81 or 82           => WeatherCondition.Rainy,
        85 or 86                 => WeatherCondition.Snowy,
        >= 95 and <= 99          => WeatherCondition.Stormy,
        _                        => WeatherCondition.Cloudy
    };
}
