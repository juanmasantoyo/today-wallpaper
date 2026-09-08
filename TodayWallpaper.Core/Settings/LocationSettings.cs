namespace TodayWallpaper.Core.Settings;

/// <summary>Manual geographic location override supplied by the user.</summary>
/// <param name="Latitude">WGS-84 latitude.</param>
/// <param name="Longitude">WGS-84 longitude.</param>
/// <param name="City">Optional human-readable city label.</param>
public record LocationSettings(double Latitude, double Longitude, string? City);
