using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Win32.TaskScheduler;
using TodayWallpaper.Core.Settings;

namespace TodayWallpaper.Core.TaskScheduler;

/// <summary>
/// Registers or updates the TodayWallpaper task in Windows Task Scheduler.
/// Uses the <c>TaskScheduler</c> NuGet package by dahall.
/// </summary>
public class TaskSchedulerRegistration(ILogger<TaskSchedulerRegistration> logger)
{
    /// <summary>Task name used in Windows Task Scheduler.</summary>
    public const string TaskName = "TodayWallpaper";

    /// <summary>
    /// Creates or updates the scheduled task based on AppSettings.
    /// </summary>
    public void Register(string workerExePath, AppSettings settings)
    {
        Register(
            workerExePath,
            settings.ScheduleMode,
            settings.RefreshIntervalHours,
            settings.DailyAtHour,
            settings.DailyAtMinute,
            settings.RunOnStartupIfExpired);
    }

    /// <summary>
    /// Creates or updates the scheduled task based on specific schedule parameters.
    /// </summary>
    public void Register(
        string workerExePath,
        ScheduleMode scheduleMode,
        int intervalHours,
        int dailyAtHour,
        int dailyAtMinute,
        bool runOnStartupIfExpired)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            logger.LogWarning("Task Scheduler registration skipped — not running on Windows.");
            return;
        }

        try
        {
            using var ts = new TaskService();
            var td = ts.NewTask();
            td.RegistrationInfo.Description = Localization.Strings.CORE_TASK_DESCRIPTION;
            td.Settings.StartWhenAvailable  = true;
            td.Settings.RunOnlyIfNetworkAvailable = true;

            if (scheduleMode == ScheduleMode.Interval)
            {
                var trigger = new TimeTrigger
                {
                    StartBoundary = DateTime.Today.AddHours(DateTime.Now.Hour + 1),
                    Repetition    = { Interval = TimeSpan.FromHours(Math.Max(1, intervalHours)), StopAtDurationEnd = false }
                };
                td.Triggers.Add(trigger);
            }
            else // ScheduleMode.Daily
            {
                var trigger = new DailyTrigger
                {
                    StartBoundary = DateTime.Today.Add(new TimeSpan(dailyAtHour, dailyAtMinute, 0)),
                    DaysInterval  = 1
                };
                td.Triggers.Add(trigger);
            }

            if (runOnStartupIfExpired)
            {
                td.Triggers.Add(new LogonTrigger());
            }

            td.Actions.Add(new ExecAction(workerExePath));

            ts.RootFolder.RegisterTaskDefinition(TaskName, td);
            logger.LogInformation(
                "Task Scheduler: '{Task}' registered. Mode={Mode}, Interval={Interval}h, Daily={Hour:D2}:{Min:D2}, OnStartup={OnStartup}.",
                TaskName, scheduleMode, intervalHours, dailyAtHour, dailyAtMinute, runOnStartupIfExpired);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to register Windows Task Scheduler task.");
        }
    }

    /// <summary>
    /// Overload for backwards compatibility: run every <paramref name="intervalHours"/> hours.
    /// </summary>
    public void Register(string workerExePath, int intervalHours)
    {
        Register(workerExePath, ScheduleMode.Interval, intervalHours, 8, 0, true);
    }

    /// <summary>Removes the TodayWallpaper task from Task Scheduler.</summary>
    public void Unregister()
    {
        try
        {
            using var ts = new TaskService();
            ts.RootFolder.DeleteTask(TaskName, exceptionOnNotExists: false);
            logger.LogInformation("Task Scheduler: '{Task}' removed.", TaskName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to remove Windows Task Scheduler task.");
        }
    }
}
