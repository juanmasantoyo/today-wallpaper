using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace TodayWallpaper.Core.Weather;

/// <summary>
/// Fetches weather data from the Open-Meteo API.
/// Open-Meteo is free, requires no API key, and has no rate limits.
/// </summary>
public sealed class WeatherService(
    IHttpClientFactory httpClientFactory,
    ILogger<WeatherService> logger) : IWeatherService
{
    private const string ApiBase = "https://api.open-meteo.com/v1/forecast";
    private const string CurrentFields =
        "temperature_2m,apparent_temperature,weathercode,windspeed_10m," +
        "relativehumidity_2m,precipitation,cloudcover,visibility,dewpoint_2m";

    /// <inheritdoc/>
    public async Task<WeatherData> GetCurrentWeatherAsync(
        double latitude,
        double longitude,
        string locationName,
        CancellationToken cancellationToken = default)
    {
        var url = FormattableString.Invariant(
            $"{ApiBase}?latitude={latitude}&longitude={longitude}&current={CurrentFields}&forecast_days=1");

        logger.LogInformation("Fetching weather for {Location} ({Lat:F4}, {Lon:F4})", locationName, latitude, longitude);

        var client = httpClientFactory.CreateClient(nameof(WeatherService));
        var response = await client.GetFromJsonAsync<OpenMeteoResponse>(url, cancellationToken)
            ?? throw new InvalidOperationException("Open-Meteo returned a null response.");

        var current = response.Current
            ?? throw new InvalidOperationException("Open-Meteo response is missing the 'current' block.");

        var condition = WmoCodeMapper.Map(current.WeatherCode);

        logger.LogInformation(
            "Weather at {Location}: {Condition} (WMO {Code}), {Temp:F1}°C, wind {Wind:F1} km/h",
            locationName, condition, current.WeatherCode, current.Temperature, current.WindSpeed);

        return new WeatherData(
            Condition:                  condition,
            WmoCode:                    current.WeatherCode,
            TemperatureCelsius:         current.Temperature,
            ApparentTemperatureCelsius: current.ApparentTemperature,
            WindspeedKmh:               current.WindSpeed,
            Humidity:                   current.RelativeHumidity,
            PrecipitationMm:            current.Precipitation,
            CloudCoverPercent:          current.CloudCover,
            VisibilityKm:               current.VisibilityMeters / 1000.0,
            DewpointCelsius:            current.DewPoint,
            LocationName:               locationName);
    }
}
