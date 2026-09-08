using Microsoft.Extensions.Logging;

namespace TodayWallpaper.Worker;

/// <summary>
/// Worker TaskSchedulerRegistration deriving from Core implementation.
/// </summary>
public sealed class TaskSchedulerRegistration(ILogger<TaskSchedulerRegistration> logger)
    : TodayWallpaper.Core.TaskScheduler.TaskSchedulerRegistration(logger)
{
}
