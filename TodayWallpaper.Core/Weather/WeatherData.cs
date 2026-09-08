namespace TodayWallpaper.Core.Weather;

/// <summary>All weather parameters fetched from the Open-Meteo API for a given moment.</summary>
/// <param name="Condition">Coarse condition bucket derived from the WMO code.</param>
/// <param name="WmoCode">Exact WMO weather interpretation code (0-99).</param>
/// <param name="TemperatureCelsius">Air temperature at 2 m above ground (°C).</param>
/// <param name="ApparentTemperatureCelsius">Feels-like temperature (°C).</param>
/// <param name="WindspeedKmh">Wind speed at 10 m (km/h).</param>
/// <param name="Humidity">Relative humidity at 2 m (%).</param>
/// <param name="PrecipitationMm">Total precipitation (mm).</param>
/// <param name="CloudCoverPercent">Total cloud cover (0-100 %).</param>
/// <param name="VisibilityKm">Horizontal visibility (km).</param>
/// <param name="DewpointCelsius">Dew-point temperature at 2 m (°C).</param>
/// <param name="LocationName">Human-readable location label.</param>
public record WeatherData(
    WeatherCondition Condition,
    int WmoCode,
    double TemperatureCelsius,
    double ApparentTemperatureCelsius,
    double WindspeedKmh,
    double Humidity,
    double PrecipitationMm,
    double CloudCoverPercent,
    double VisibilityKm,
    double DewpointCelsius,
    string LocationName);
