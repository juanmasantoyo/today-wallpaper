namespace TodayWallpaper.Core.Weather;

/// <summary>Resolves the user's approximate geographic location from their public IP address.</summary>
public interface IGeolocationService
{
    /// <summary>
    /// Returns the current location, using a cached value when available.
    /// The result is persisted in settings to avoid repeated external calls.
    /// </summary>
    Task<LocationInfo> GetLocationAsync(CancellationToken cancellationToken = default);
}

/// <summary>Location data resolved from an IP geolocation API.</summary>
/// <param name="Latitude">WGS-84 latitude.</param>
/// <param name="Longitude">WGS-84 longitude.</param>
/// <param name="City">City name (may be empty for manual overrides).</param>
/// <param name="Country">ISO country name or code.</param>
public record LocationInfo(double Latitude, double Longitude, string City, string Country);
