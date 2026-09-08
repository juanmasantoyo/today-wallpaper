using TodayWallpaper.Core.Weather;

namespace TodayWallpaper.Core.History;

/// <summary>A single entry in the wallpaper history ring buffer.</summary>
/// <param name="Id">ISO-8601 timestamp used as unique identifier.</param>
/// <param name="File">Relative path to the JPEG file inside the history folder.</param>
/// <param name="Condition">Weather condition at generation time.</param>
/// <param name="Temperature">Air temperature at generation time (°C).</param>
/// <param name="Style">Generator style ID used.</param>
/// <param name="Pinned">When <c>true</c> this entry is never auto-deleted.</param>
public record HistoryEntry(
    string Id,
    string File,
    WeatherCondition Condition,
    double Temperature,
    string Style,
    bool Pinned = false);
