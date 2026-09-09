using Microsoft.Extensions.DependencyInjection;
using TodayWallpaper.Core.Generators;
using TodayWallpaper.Core.History;
using TodayWallpaper.Core.Palette;
using TodayWallpaper.Core.Settings;
using TodayWallpaper.Core.Wallpaper;
using TodayWallpaper.Core.Weather;

namespace TodayWallpaper.Core;

/// <summary>Extension methods for registering TodayWallpaper.Core services with the DI container.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Core services: weather, palette, generators, wallpaper and history.
    /// </summary>
    public static IServiceCollection AddTodayWallpaperCore(this IServiceCollection services)
    {
        // HTTP clients.
        services.AddHttpClient(nameof(WeatherService));
        services.AddHttpClient(nameof(GeolocationService));

        // Settings.
        services.AddSingleton<ISettingsStore, SettingsStore>();

        // Weather.
        services.AddTransient<IWeatherService, WeatherService>();
        services.AddTransient<IGeolocationService, GeolocationService>();

        // Palette.
        services.AddSingleton<IPaletteConfigLoader, PaletteConfigLoader>();
        services.AddTransient<IPaletteMapper, PaletteMapper>();

        // Generators — all six styles registered as IWallpaperGenerator.
        services.AddTransient<IWallpaperGenerator, BlurBlobsGenerator>();
        services.AddTransient<IWallpaperGenerator, LowPolyGenerator>();
        services.AddTransient<IWallpaperGenerator, RadialGradientGenerator>();
        services.AddTransient<IWallpaperGenerator, AuroraWavesGenerator>();
        services.AddTransient<IWallpaperGenerator, VoronoiMosaicGenerator>();
        services.AddTransient<IWallpaperGenerator, AtmosphericRidgesGenerator>();
        services.AddTransient<WallpaperGeneratorFactory>();

        // Wallpaper.
        services.AddTransient<IWallpaperSetter, WallpaperSetter>();
        services.AddTransient<IScreenResolutionProvider, ScreenResolutionProvider>();
        services.AddTransient<WallpaperPipeline>();
        services.AddTransient<TaskScheduler.TaskSchedulerRegistration>();

        // History.
        services.AddSingleton<IHistoryStore, HistoryStore>();

        return services;
    }
}
