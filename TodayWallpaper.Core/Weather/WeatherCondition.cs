namespace TodayWallpaper.Core.Weather;

/// <summary>Weather condition bucket derived from a WMO weather interpretation code.</summary>
public enum WeatherCondition
{
    /// <summary>Clear sky (WMO 0).</summary>
    Sunny,
    /// <summary>Mainly cloudy (WMO 1-3).</summary>
    Cloudy,
    /// <summary>Fog or depositing rime fog (WMO 45, 48).</summary>
    Foggy,
    /// <summary>Drizzle or rain (WMO 51-67, 80-82).</summary>
    Rainy,
    /// <summary>Snow fall or snow grains (WMO 71-77, 85-86).</summary>
    Snowy,
    /// <summary>Thunderstorm (WMO 95-99).</summary>
    Stormy
}
