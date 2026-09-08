using System.Text.Json.Serialization;

namespace TodayWallpaper.Core.Weather;

/// <summary>Root DTO for the Open-Meteo forecast API JSON response.</summary>
public sealed class OpenMeteoResponse
{
    /// <summary>Current weather snapshot.</summary>
    [JsonPropertyName("current")]
    public CurrentWeatherDto? Current { get; init; }
}

/// <summary>Current-weather block inside an Open-Meteo response.</summary>
public sealed class CurrentWeatherDto
{
    /// <summary>Air temperature at 2 m (°C).</summary>
    [JsonPropertyName("temperature_2m")]
    public double Temperature { get; init; }

    /// <summary>Apparent (feels-like) temperature (°C).</summary>
    [JsonPropertyName("apparent_temperature")]
    public double ApparentTemperature { get; init; }

    /// <summary>WMO weather interpretation code.</summary>
    [JsonPropertyName("weathercode")]
    public int WeatherCode { get; init; }

    /// <summary>Wind speed at 10 m (km/h).</summary>
    [JsonPropertyName("windspeed_10m")]
    public double WindSpeed { get; init; }

    /// <summary>Relative humidity at 2 m (%).</summary>
    [JsonPropertyName("relativehumidity_2m")]
    public double RelativeHumidity { get; init; }

    /// <summary>Accumulated precipitation (mm).</summary>
    [JsonPropertyName("precipitation")]
    public double Precipitation { get; init; }

    /// <summary>Cloud cover (%).</summary>
    [JsonPropertyName("cloudcover")]
    public double CloudCover { get; init; }

    /// <summary>Visibility (m — converted to km on mapping).</summary>
    [JsonPropertyName("visibility")]
    public double VisibilityMeters { get; init; }

    /// <summary>Dew-point temperature at 2 m (°C).</summary>
    [JsonPropertyName("dewpoint_2m")]
    public double DewPoint { get; init; }
}
