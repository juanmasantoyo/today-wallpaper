using Microsoft.Extensions.Logging;
using Moq;
using TodayWallpaper.App.ViewModels;
using TodayWallpaper.Core.Generators;
using TodayWallpaper.Core.History;
using TodayWallpaper.Core.Palette;
using TodayWallpaper.Core.Settings;
using TodayWallpaper.Core.TaskScheduler;
using TodayWallpaper.Core.Wallpaper;
using TodayWallpaper.Core.Weather;
using Xunit;

namespace TodayWallpaper.Tests.Settings;

public class AutoSaveTests
{
    private readonly Mock<ISettingsStore> _settingsStoreMock = new();
    private readonly Mock<IHistoryStore> _historyStoreMock = new();
    private readonly Mock<IWallpaperSetter> _wallpaperSetterMock = new();
    private readonly Mock<IGeolocationService> _geolocationServiceMock = new();
    private readonly Mock<IWeatherService> _weatherServiceMock = new();
    private readonly Mock<IPaletteConfigLoader> _paletteConfigLoaderMock = new();
    private readonly Mock<ILogger<MainViewModel>> _loggerMock = new();

    private MainViewModel CreateViewModel()
    {
        var factory = new WallpaperGeneratorFactory(
            [
                new BlurBlobsGenerator(Mock.Of<ILogger<BlurBlobsGenerator>>()),
                new LowPolyGenerator(Mock.Of<ILogger<LowPolyGenerator>>()),
                new RadialGradientGenerator(Mock.Of<ILogger<RadialGradientGenerator>>()),
                new AuroraWavesGenerator(Mock.Of<ILogger<AuroraWavesGenerator>>()),
                new VoronoiMosaicGenerator(Mock.Of<ILogger<VoronoiMosaicGenerator>>()),
                new AtmosphericRidgesGenerator(Mock.Of<ILogger<AtmosphericRidgesGenerator>>())
            ],
            Mock.Of<ILogger<WallpaperGeneratorFactory>>());

        _settingsStoreMock.Setup(s => s.LoadAsync(default))
            .ReturnsAsync(new AppSettings());

        _historyStoreMock.Setup(h => h.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<HistoryEntry>());

        _paletteConfigLoaderMock.Setup(p => p.GetDefaultConfig())
            .Returns(new PaletteConfigLoader(Mock.Of<ILogger<PaletteConfigLoader>>()).GetDefaultConfig());
        _paletteConfigLoaderMock.Setup(p => p.Load())
            .Returns(new PaletteConfigLoader(Mock.Of<ILogger<PaletteConfigLoader>>()).GetDefaultConfig());

        var taskRegistration = new TaskSchedulerRegistration(Mock.Of<ILogger<TaskSchedulerRegistration>>());
        var paletteMapper = new PaletteMapper(_paletteConfigLoaderMock.Object);
        var screenResolution = new ScreenResolutionProvider();

        var pipeline = new WallpaperPipeline(
            _geolocationServiceMock.Object,
            _weatherServiceMock.Object,
            paletteMapper,
            factory,
            _wallpaperSetterMock.Object,
            screenResolution,
            _historyStoreMock.Object,
            _settingsStoreMock.Object,
            Mock.Of<ILogger<WallpaperPipeline>>());

        return new MainViewModel(
            _settingsStoreMock.Object,
            _historyStoreMock.Object,
            _wallpaperSetterMock.Object,
            pipeline,
            _geolocationServiceMock.Object,
            _weatherServiceMock.Object,
            factory,
            taskRegistration,
            _paletteConfigLoaderMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task InitializeAsync_DoesNotTriggerAutoSave()
    {
        var vm = CreateViewModel();

        await vm.InitializeAsync();

        // Ensure SaveAsync was not called during initialization
        _settingsStoreMock.Verify(s => s.SaveAsync(It.IsAny<AppSettings>(), default), Times.Never);
        _paletteConfigLoaderMock.Verify(p => p.Save(It.IsAny<PaletteConfig>()), Times.Never);
    }

    [Fact]
    public async Task PropertyChanged_TriggersDebouncedSettingsSave()
    {
        var vm = CreateViewModel();
        await vm.InitializeAsync();

        // Change a setting property
        vm.RefreshIntervalHours = 6;

        // Wait for the 500ms debounce to fire
        await Task.Delay(750);

        _settingsStoreMock.Verify(s => s.SaveAsync(It.Is<AppSettings>(st => st.RefreshIntervalHours == 6), default), Times.AtLeastOnce);
    }

    [Fact]
    public async Task RapidPropertyChanges_DebounceIntoSingleSave()
    {
        var vm = CreateViewModel();
        await vm.InitializeAsync();

        _settingsStoreMock.Invocations.Clear();

        // Make rapid successive changes
        vm.DailyAtHour = 10;
        await Task.Delay(50);
        vm.DailyAtHour = 11;
        await Task.Delay(50);
        vm.DailyAtHour = 12;

        // Wait for debounce to complete
        await Task.Delay(750);

        // Verify only 1 save occurred for the final value (12)
        _settingsStoreMock.Verify(s => s.SaveAsync(It.Is<AppSettings>(st => st.DailyAtHour == 12), default), Times.Once);
    }

    [Fact]
    public async Task FlushPendingSavesAsync_ImmediatelyPersistsPendingChanges()
    {
        var vm = CreateViewModel();
        await vm.InitializeAsync();

        _settingsStoreMock.Invocations.Clear();

        vm.HistoryMaxCount = 80;

        // Flush immediately without waiting 500ms
        await vm.FlushPendingSavesAsync();

        _settingsStoreMock.Verify(s => s.SaveAsync(It.Is<AppSettings>(st => st.HistoryMaxCount == 80), default), Times.Once);
    }

    [Fact]
    public async Task AddPrimaryColor_TriggersDebouncedPaletteSave()
    {
        var vm = CreateViewModel();
        await vm.InitializeAsync();

        _paletteConfigLoaderMock.Invocations.Clear();

        vm.AddPrimaryColor();

        // Wait for debounce
        await Task.Delay(750);

        _paletteConfigLoaderMock.Verify(p => p.Save(It.IsAny<PaletteConfig>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ResetAllPalettes_TriggersImmediatePaletteSave()
    {
        var vm = CreateViewModel();
        await vm.InitializeAsync();

        _paletteConfigLoaderMock.Invocations.Clear();

        vm.ResetAllPalettes();

        // Wait brief instant
        await Task.Delay(100);

        _paletteConfigLoaderMock.Verify(p => p.Save(It.IsAny<PaletteConfig>()), Times.AtLeastOnce);
    }
}
