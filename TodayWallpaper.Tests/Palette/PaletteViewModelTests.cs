using Microsoft.Extensions.Logging;
using Moq;
using SkiaSharp;
using TodayWallpaper.App.ViewModels;
using TodayWallpaper.Core.Generators;
using TodayWallpaper.Core.History;
using TodayWallpaper.Core.Palette;
using TodayWallpaper.Core.Settings;
using TodayWallpaper.Core.TaskScheduler;
using TodayWallpaper.Core.Wallpaper;
using TodayWallpaper.Core.Weather;
using Xunit;

namespace TodayWallpaper.Tests.Palette;

public class PaletteViewModelTests
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
        _settingsStoreMock.Setup(s => s.LoadAsync(default)).ReturnsAsync(new AppSettings());
        _paletteConfigLoaderMock.Setup(l => l.Load()).Returns(DefaultPalettes.Config);
        _paletteConfigLoaderMock.Setup(l => l.GetDefaultConfig()).Returns(DefaultPalettes.Config);

        var factory = new WallpaperGeneratorFactory([], Mock.Of<ILogger<WallpaperGeneratorFactory>>());

        var pipeline = new WallpaperPipeline(
            _geolocationServiceMock.Object,
            _weatherServiceMock.Object,
            new PaletteMapper(_paletteConfigLoaderMock.Object),
            factory,
            _wallpaperSetterMock.Object,
            new ScreenResolutionProvider(),
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
            new TaskSchedulerRegistration(Mock.Of<ILogger<TaskSchedulerRegistration>>()),
            _paletteConfigLoaderMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public void LoadPalettes_LoadsActivePalettesAndSelectsFirst()
    {
        var vm = CreateViewModel();
        vm.LoadPalettes();

        Assert.NotEmpty(vm.ActivePalettes);
        Assert.NotNull(vm.SelectedPalette);
        Assert.Equal("Default", vm.SelectedPalette.Name);
        Assert.True(vm.SelectedPalette.IsEnabled);
        Assert.NotEmpty(vm.SelectedPalette.PrimaryColors);
        Assert.NotEmpty(vm.SelectedPalette.SecondaryColors);
    }

    [Fact]
    public void AddNewPalette_CreatesAndSelectsNewPalette()
    {
        var vm = CreateViewModel();
        vm.LoadPalettes();

        var initialCount = vm.ActivePalettes.Count;
        vm.AddNewPalette();

        Assert.Equal(initialCount + 1, vm.ActivePalettes.Count);
        Assert.NotNull(vm.SelectedPalette);
        Assert.StartsWith("Paleta", vm.SelectedPalette.Name);
        Assert.True(vm.CanDeletePalette);
    }

    [Fact]
    public void DuplicateCurrentPalette_ClonesCurrentColorsAndSelectsDuplicate()
    {
        var vm = CreateViewModel();
        vm.LoadPalettes();

        var original = vm.SelectedPalette!;
        var originalPrimaryCount = original.PrimaryColors.Count;

        vm.DuplicateCurrentPalette();

        Assert.NotNull(vm.SelectedPalette);
        Assert.Contains("(Copia)", vm.SelectedPalette.Name);
        Assert.Equal(originalPrimaryCount, vm.SelectedPalette.PrimaryColors.Count);
    }

    [Fact]
    public void DeleteCurrentPalette_RemovesPaletteWhenMoreThanOne()
    {
        var vm = CreateViewModel();
        vm.LoadPalettes();
        vm.AddNewPalette();

        var countBefore = vm.ActivePalettes.Count;
        vm.DeleteCurrentPalette();

        Assert.Equal(countBefore - 1, vm.ActivePalettes.Count);
        Assert.NotNull(vm.SelectedPalette);
    }

    [Fact]
    public void MoveColor_MovesItemBetweenPrimaryAndSecondary()
    {
        var vm = CreateViewModel();
        vm.LoadPalettes();

        var palette = vm.SelectedPalette!;
        var colorToMove = palette.PrimaryColors[0];
        var initialPrimaryCount = palette.PrimaryColors.Count;
        var initialSecondaryCount = palette.SecondaryColors.Count;

        // Move from Primary to Secondary
        vm.MoveColor(colorToMove, toSecondary: true);

        Assert.Equal(initialPrimaryCount - 1, palette.PrimaryColors.Count);
        Assert.Equal(initialSecondaryCount + 1, palette.SecondaryColors.Count);
        Assert.Contains(colorToMove, palette.SecondaryColors);
        Assert.DoesNotContain(colorToMove, palette.PrimaryColors);

        // Move back to Primary
        vm.MoveColor(colorToMove, toSecondary: false);

        Assert.Equal(initialPrimaryCount, palette.PrimaryColors.Count);
        Assert.Equal(initialSecondaryCount, palette.SecondaryColors.Count);
        Assert.Contains(colorToMove, palette.PrimaryColors);
    }
}
