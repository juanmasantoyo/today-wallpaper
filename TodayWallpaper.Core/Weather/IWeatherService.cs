namespace TodayWallpaper.Core.Weather;

/// <summary>Provides current weather data for a geographic location.</summary>
public interface IWeatherService
{
    /// <summary>
    /// Fetches the current weather snapshot for the given coordinates.
    /// </summary>
    /// <param name="latitude">WGS-84 latitude.</param>
    /// <param name="longitude">WGS-84 longitude.</param>
    /// <param name="locationName">Human-readable label for logging.</param>
    /// <param name="cancellationToken">Propagates cancellation.</param>
    Task<WeatherData> GetCurrentWeatherAsync(
        double latitude,
        double longitude,
        string locationName,
        CancellationToken cancellationToken = default);
}
