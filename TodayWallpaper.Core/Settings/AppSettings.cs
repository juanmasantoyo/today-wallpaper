using TodayWallpaper.Core.Palette;
using TodayWallpaper.Core.Weather;

namespace TodayWallpaper.Core.Settings;

/// <summary>All user-configurable and persisted application settings.</summary>
public record AppSettings
{
    /// <summary>Scheduling mode: Interval or Daily.</summary>
    public ScheduleMode ScheduleMode { get; init; } = ScheduleMode.Interval;

    /// <summary>How often (in hours) the wallpaper is regenerated when using Interval mode. Range: 1-24.</summary>
    public int RefreshIntervalHours { get; init; } = 2;

    /// <summary>Hour of day (0-23) for Daily schedule mode.</summary>
    public int DailyAtHour { get; init; } = 8;

    /// <summary>Minute of day (0-59) for Daily schedule mode.</summary>
    public int DailyAtMinute { get; init; } = 0;

    /// <summary>When true, checks on system startup/logon and regenerates if the last generation has expired.</summary>
    public bool RunOnStartupIfExpired { get; init; } = true;

    /// <summary>Timestamp (UTC) of the last successful wallpaper generation.</summary>
    public DateTime? LastGenerationTimeUtc { get; init; }

    /// <summary>Color palette mapping mode.</summary>
    public PaletteMode PaletteMode { get; init; } = PaletteMode.Empathic;

    /// <summary>Default list of all available generator style IDs.</summary>
    public static readonly IReadOnlyList<string> DefaultGeneratorStyles =
        ["BlurBlobs", "LowPoly", "RadialGradient", "AuroraWaves", "VoronoiMosaic", "AtmosphericRidges"];

    private readonly IReadOnlyList<string>? _selectedGeneratorStyles;

    /// <summary>Selected generator style IDs to pick from randomly during generation.</summary>
    public IReadOnlyList<string> SelectedGeneratorStyles
    {
        get
        {
            if (_selectedGeneratorStyles is { Count: > 0 })
                return _selectedGeneratorStyles;

            if (!string.IsNullOrEmpty(GeneratorStyle) && !string.Equals(GeneratorStyle, "Random", StringComparison.OrdinalIgnoreCase))
                return [GeneratorStyle];

            return DefaultGeneratorStyles;
        }
        init => _selectedGeneratorStyles = value;
    }

    /// <summary>Legacy single-style property for backwards compatibility with older settings files.</summary>
    public string? GeneratorStyle { get; init; }

    /// <summary>When <c>false</c> automatic wallpaper generation is paused.</summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>Manual location override. <c>null</c> = use automatic IP-based geolocation.</summary>
    public LocationSettings? Location { get; init; }

    /// <summary>Maximum number of wallpapers to retain in the rolling history.</summary>
    public int HistoryMaxCount { get; init; } = 50;

    /// <summary>IP-geolocation result cached from a previous run. <c>null</c> = not yet resolved.</summary>
    public LocationInfo? CachedLocation { get; init; }

    /// <summary>Summary of the most recently retrieved weather data.</summary>
    public WeatherData? CachedWeatherData { get; init; }

    /// <summary>
    /// Evaluates whether the wallpaper generation has expired according to the active schedule.
    /// </summary>
    public bool IsExpired(DateTime? referenceTimeUtc = null)
    {
        if (!IsEnabled) return false;
        if (LastGenerationTimeUtc is null) return true;

        var nowUtc = referenceTimeUtc ?? DateTime.UtcNow;
        var lastUtc = LastGenerationTimeUtc.Value;

        // 60-second tolerance to prevent timer jitter or clock drift
        var tolerance = TimeSpan.FromMinutes(1);

        if (ScheduleMode == ScheduleMode.Interval)
        {
            var interval = TimeSpan.FromHours(Math.Max(1, RefreshIntervalHours)) - tolerance;
            return (nowUtc - lastUtc) >= interval;
        }

        // Daily mode evaluated in local time
        var nowLocal = nowUtc.ToLocalTime();
        var lastLocal = lastUtc.ToLocalTime();

        var scheduledToday = nowLocal.Date.Add(new TimeSpan(DailyAtHour, DailyAtMinute, 0));
        var mostRecentScheduledBoundary = (nowLocal >= (scheduledToday - tolerance))
            ? scheduledToday
            : scheduledToday.AddDays(-1);

        return lastLocal < (mostRecentScheduledBoundary - tolerance);
    }
}
