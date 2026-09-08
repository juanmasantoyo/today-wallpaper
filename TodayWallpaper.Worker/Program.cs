using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TodayWallpaper.Core;
using TodayWallpaper.Core.Wallpaper;
using TodayWallpaper.Worker;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddTodayWallpaperCore();
        services.AddTransient<TaskSchedulerRegistration>();
    })
    .Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();

try
{
    var force = args.Contains("--force") || args.Contains("-f");
    logger.LogInformation("TodayWallpaper Worker starting (force={Force})...", force);
    var pipeline = host.Services.GetRequiredService<WallpaperPipeline>();
    await pipeline.RunAsync(force: force);
    logger.LogInformation("TodayWallpaper Worker finished.");
}
catch (OperationCanceledException)
{
    logger.LogInformation("Worker cancelled.");
}
catch (Exception ex)
{
    logger.LogCritical(ex, "Unhandled exception in TodayWallpaper Worker.");
    Environment.Exit(1);
}
