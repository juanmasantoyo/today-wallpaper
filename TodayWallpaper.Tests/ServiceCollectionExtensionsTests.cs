using Microsoft.Extensions.DependencyInjection;
using TodayWallpaper.Core;
using TodayWallpaper.Core.Generators;
using TodayWallpaper.Core.History;
using TodayWallpaper.Core.Palette;
using TodayWallpaper.Core.Settings;
using TodayWallpaper.Core.Wallpaper;
using TodayWallpaper.Core.Weather;
using Xunit;

namespace TodayWallpaper.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddTodayWallpaperCore_RegistersAllServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTodayWallpaperCore();

        var sp = services.BuildServiceProvider();

        Assert.NotNull(sp.GetService<ISettingsStore>());
        Assert.NotNull(sp.GetService<IWeatherService>());
        Assert.NotNull(sp.GetService<IGeolocationService>());
        Assert.NotNull(sp.GetService<IPaletteConfigLoader>());
        Assert.NotNull(sp.GetService<IPaletteMapper>());
        Assert.NotNull(sp.GetService<WallpaperGeneratorFactory>());
        Assert.NotNull(sp.GetService<IWallpaperSetter>());
        Assert.NotNull(sp.GetService<IScreenResolutionProvider>());
        Assert.NotNull(sp.GetService<IHistoryStore>());

        var generators = sp.GetServices<IWallpaperGenerator>().ToList();
        Assert.Equal(5, generators.Count);
    }
}
