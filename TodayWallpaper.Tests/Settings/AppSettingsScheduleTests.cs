using TodayWallpaper.Core.Settings;
using Xunit;

namespace TodayWallpaper.Tests.Settings;

public class AppSettingsScheduleTests
{
    [Fact]
    public void AppSettings_DefaultValues_AreCorrect()
    {
        var settings = new AppSettings();

        Assert.Equal(ScheduleMode.Interval, settings.ScheduleMode);
        Assert.Equal(2, settings.RefreshIntervalHours);
        Assert.Equal(8, settings.DailyAtHour);
        Assert.Equal(0, settings.DailyAtMinute);
        Assert.True(settings.RunOnStartupIfExpired);
        Assert.Null(settings.LastGenerationTimeUtc);
        Assert.Null(settings.CachedWeatherData);
    }

    [Fact]
    public void IsExpired_WhenDisabled_ReturnsFalse()
    {
        var settings = new AppSettings
        {
            IsEnabled = false,
            LastGenerationTimeUtc = DateTime.UtcNow.AddDays(-10)
        };

        Assert.False(settings.IsExpired());
    }

    [Fact]
    public void IsExpired_WhenNeverGenerated_ReturnsTrue()
    {
        var settings = new AppSettings
        {
            IsEnabled = true,
            LastGenerationTimeUtc = null
        };

        Assert.True(settings.IsExpired());
    }

    [Fact]
    public void IsExpired_IntervalMode_RecentGeneration_ReturnsFalse()
    {
        var now = DateTime.UtcNow;
        var settings = new AppSettings
        {
            IsEnabled = true,
            ScheduleMode = ScheduleMode.Interval,
            RefreshIntervalHours = 2,
            LastGenerationTimeUtc = now.AddMinutes(-30) // 30 min ago, interval is 2h
        };

        Assert.False(settings.IsExpired(now));
    }

    [Fact]
    public void IsExpired_IntervalMode_ExpiredGeneration_ReturnsTrue()
    {
        var now = DateTime.UtcNow;
        var settings = new AppSettings
        {
            IsEnabled = true,
            ScheduleMode = ScheduleMode.Interval,
            RefreshIntervalHours = 2,
            LastGenerationTimeUtc = now.AddHours(-3) // 3h ago, interval is 2h
        };

        Assert.True(settings.IsExpired(now));
    }

    [Fact]
    public void IsExpired_DailyMode_BeforeScheduledHour_GeneratedYesterdayAfterTarget_ReturnsFalse()
    {
        // Local time: today at 07:00. Target is 08:00.
        // Most recent target was yesterday 08:00.
        // Last generation was yesterday 08:30 (after yesterday's target).
        var todayLocal = DateTime.Today;
        var nowLocal = todayLocal.AddHours(7);
        var nowUtc = nowLocal.ToUniversalTime();

        var lastLocal = todayLocal.AddDays(-1).AddHours(8.5);
        var lastUtc = lastLocal.ToUniversalTime();

        var settings = new AppSettings
        {
            IsEnabled = true,
            ScheduleMode = ScheduleMode.Daily,
            DailyAtHour = 8,
            DailyAtMinute = 0,
            LastGenerationTimeUtc = lastUtc
        };

        Assert.False(settings.IsExpired(nowUtc));
    }

    [Fact]
    public void IsExpired_DailyMode_PastScheduledHour_GeneratedBeforeTargetToday_ReturnsTrue()
    {
        // Local time: today at 10:00. Target was 08:00 today.
        // Last generation was yesterday at 08:30.
        var todayLocal = DateTime.Today;
        var nowLocal = todayLocal.AddHours(10);
        var nowUtc = nowLocal.ToUniversalTime();

        var lastLocal = todayLocal.AddDays(-1).AddHours(8.5);
        var lastUtc = lastLocal.ToUniversalTime();

        var settings = new AppSettings
        {
            IsEnabled = true,
            ScheduleMode = ScheduleMode.Daily,
            DailyAtHour = 8,
            DailyAtMinute = 0,
            LastGenerationTimeUtc = lastUtc
        };

        Assert.True(settings.IsExpired(nowUtc));
    }
}
