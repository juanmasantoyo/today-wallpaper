using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using TodayWallpaper.Core.Settings;

namespace TodayWallpaper.Core.Weather;

/// <summary>
/// Resolves approximate geolocation via <c>ip-api.com</c> (free, no API key required).
/// The resolved location is cached in application settings to avoid repeated calls.
/// </summary>
public sealed class GeolocationService(
    IHttpClientFactory httpClientFactory,
    ISettingsStore settingsStore,
    ILogger<GeolocationService> logger) : IGeolocationService
{
    private const string ApiUrl = "http://ip-api.com/json";

    /// <inheritdoc/>
    public async Task<LocationInfo> GetLocationAsync(CancellationToken cancellationToken = default)
    {
        var settings = await settingsStore.LoadAsync(cancellationToken);

        // Manual override takes highest priority.
        if (settings.Location is { } manual)
        {
            logger.LogDebug("Using manually configured location: {City}", manual.City);
            return new LocationInfo(manual.Latitude, manual.Longitude, manual.City ?? "Custom", string.Empty);
        }

        // Return previously cached result.
        if (settings.CachedLocation is { } cached)
        {
            logger.LogDebug("Using cached IP geolocation: {City}, {Country}", cached.City, cached.Country);
            return cached;
        }

        // Fetch from ip-api.com.
        logger.LogInformation("Resolving location from public IP via ip-api.com");
        var client = httpClientFactory.CreateClient(nameof(GeolocationService));
        var dto = await client.GetFromJsonAsync<IpApiDto>(ApiUrl, cancellationToken)
            ?? throw new InvalidOperationException("ip-api.com returned a null response.");

        var info = new LocationInfo(dto.Lat, dto.Lon, dto.City ?? "Unknown", dto.Country ?? string.Empty);

        // Cache for future runs.
        var updated = settings with { CachedLocation = info };
        await settingsStore.SaveAsync(updated, cancellationToken);

        logger.LogInformation(
            "Location resolved: {City}, {Country} ({Lat:F4}, {Lon:F4})",
            info.City, info.Country, info.Latitude, info.Longitude);

        return info;
    }

    private sealed class IpApiDto
    {
        public double Lat { get; init; }
        public double Lon { get; init; }
        public string? City { get; init; }
        public string? Country { get; init; }
    }
}
