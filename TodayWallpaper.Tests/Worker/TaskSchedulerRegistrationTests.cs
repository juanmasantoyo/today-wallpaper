using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Moq;
using TodayWallpaper.Worker;
using Xunit;

namespace TodayWallpaper.Tests.Worker;

public class TaskSchedulerRegistrationTests
{
    private readonly Mock<ILogger<TaskSchedulerRegistration>> _loggerMock = new();

    [Fact]
    public void Unregister_DoesNotThrow()
    {
        var registration = new TaskSchedulerRegistration(_loggerMock.Object);
        var ex = Record.Exception(() => registration.Unregister());
        Assert.Null(ex);
    }

    [Fact]
    public void Register_DoesNotThrow()
    {
        var registration = new TaskSchedulerRegistration(_loggerMock.Object);
        var dummyPath = @"C:\Dummy\TodayWallpaper.Worker.exe";
        var ex = Record.Exception(() => registration.Register(dummyPath, 2));
        Assert.Null(ex);

        // Clean up task after registration attempt
        registration.Unregister();
    }

    [Fact]
    public void Register_WithAppSettings_DoesNotThrow()
    {
        var registration = new TaskSchedulerRegistration(_loggerMock.Object);
        var dummyPath = @"C:\Dummy\TodayWallpaper.Worker.exe";
        var settings = new TodayWallpaper.Core.Settings.AppSettings
        {
            ScheduleMode = TodayWallpaper.Core.Settings.ScheduleMode.Daily,
            DailyAtHour = 9,
            DailyAtMinute = 30,
            RunOnStartupIfExpired = true
        };

        var ex = Record.Exception(() => registration.Register(dummyPath, settings));
        Assert.Null(ex);

        // Clean up
        registration.Unregister();
    }
}
