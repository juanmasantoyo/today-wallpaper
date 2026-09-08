using TodayWallpaper.Core.Weather;

namespace TodayWallpaper.Core;

/// <summary>
/// Builds a deterministic random seed from a <see cref="WeatherData"/> snapshot.
/// The seed is unique per combination of weather parameters <em>and</em> current hour,
/// so every hourly refresh produces a distinct image even when conditions are unchanged.
/// The same snapshot at the same hour always yields the same seed (reproducible).
/// </summary>
public static class WeatherSeed
{
    /// <summary>
    /// Computes an <see cref="int"/> seed suitable for initialising a <see cref="Random"/> instance.
    /// An optional <paramref name="timestamp"/> can be supplied (defaults to <see cref="DateTime.Now"/>).
    /// </summary>
    public static int Build(WeatherData data, DateTime? timestamp = null)
    {
        var time = timestamp ?? DateTime.Now;
        var hash = new HashCode();
        hash.Add((int)data.Condition);
        hash.Add(data.WmoCode);
        hash.Add((int)data.TemperatureCelsius);
        hash.Add((int)data.WindspeedKmh);
        hash.Add((int)data.Humidity);
        hash.Add((int)data.PrecipitationMm);
        hash.Add((int)data.CloudCoverPercent);
        hash.Add((int)data.VisibilityKm);
        hash.Add(time.DayOfYear * 100 + time.Hour);
        return hash.ToHashCode();
    }
}
