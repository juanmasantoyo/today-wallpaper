namespace TodayWallpaper.Core.Settings;

/// <summary>Defines the scheduling cadence for automatic wallpaper generation.</summary>
public enum ScheduleMode
{
    /// <summary>Generates a wallpaper periodically every X hours.</summary>
    Interval,

    /// <summary>Generates a wallpaper once per day at a specific fixed time.</summary>
    Daily
}
