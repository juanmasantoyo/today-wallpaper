using Microsoft.Extensions.Logging;
using Moq;
using SkiaSharp;
using TodayWallpaper.Core.Generators;
using TodayWallpaper.Core.History;
using TodayWallpaper.Core.Palette;
using TodayWallpaper.Core.Settings;
using TodayWallpaper.Core.Wallpaper;
using TodayWallpaper.Core.Weather;
using TodayWallpaper.Worker;
using Xunit;

namespace TodayWallpaper.Tests.Worker;

public class WallpaperPipelineTests
{
    private readonly Mock<IGeolocationService> _geoMock = new();
    private readonly Mock<IWeatherService> _weatherMock = new();
    private readonly Mock<IPaletteMapper> _paletteMock = new();
    private readonly Mock<IWallpaperSetter> _setterMock = new();
    private readonly Mock<IScreenResolutionProvider> _resMock = new();
    private readonly Mock<IHistoryStore> _historyMock = new();
    private readonly Mock<ISettingsStore> _settingsMock = new();
    private readonly Mock<ILogger<WallpaperPipeline>> _loggerMock = new();
    private readonly Mock<ILogger<WallpaperGeneratorFactory>> _factoryLoggerMock = new();

    [Fact]
    public async Task RunAsync_WhenDisabled_ReturnsFalseImmediately()
    {
        _settingsMock.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppSettings { IsEnabled = false });

        var factory = new WallpaperGeneratorFactory(new[]
        {
            new BlurBlobsGenerator(new Mock<ILogger<BlurBlobsGenerator>>().Object)
        }, _factoryLoggerMock.Object);

        var pipeline = new WallpaperPipeline(
            _geoMock.Object,
            _weatherMock.Object,
            _paletteMock.Object,
            factory,
            _setterMock.Object,
            _resMock.Object,
            _historyMock.Object,
            _settingsMock.Object,
            _loggerMock.Object);

        var result = await pipeline.RunAsync();

        Assert.False(result);
        _geoMock.Verify(g => g.GetLocationAsync(It.IsAny<CancellationToken>()), Times.Never);
        _weatherMock.Verify(w => w.GetCurrentWeatherAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _setterMock.Verify(s => s.Set(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_WhenEnabled_OrchestratesFullPipeline()
    {
        var tempOutputPath = Path.Combine(Path.GetTempPath(), $"pipeline_out_{Guid.NewGuid():N}.jpg");
        await File.WriteAllBytesAsync(tempOutputPath, new byte[] { 1, 2, 3 });

        try
        {
            var settings = new AppSettings
            {
                IsEnabled = true,
                GeneratorStyle = "MockStyle",
                PaletteMode = PaletteMode.Empathic,
                HistoryMaxCount = 20
            };
            _settingsMock.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

            _geoMock.Setup(g => g.GetLocationAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LocationInfo(40.4168, -3.7038, "Madrid", "Spain"));

            var weather = new WeatherData(
                WeatherCondition.Sunny,
                0,
                25.0,
                24.5,
                10.0,
                40,
                0.0,
                5,
                15.0,
                10.0,
                "Madrid");
            _weatherMock.Setup(w => w.GetCurrentWeatherAsync(40.4168, -3.7038, "Madrid", It.IsAny<CancellationToken>()))
                .ReturnsAsync(weather);

            var palette = new ColorPalette(
                new[] { new SKColor(20, 20, 40) },
                new[] { new SKColor(60, 60, 80) },
                0.7f);
            _paletteMock.Setup(p => p.Map(WeatherCondition.Sunny, PaletteMode.Empathic, weather))
                .Returns(palette);

            var generatorMock = new Mock<IWallpaperGenerator>();
            generatorMock.Setup(g => g.StyleId).Returns("MockStyle");
            generatorMock.Setup(g => g.GenerateAsync(
                palette, weather, 1920, 1080, It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(tempOutputPath);

            var factory = new WallpaperGeneratorFactory(new[] { generatorMock.Object }, _factoryLoggerMock.Object);

            _resMock.Setup(r => r.GetWidth()).Returns(1920);
            _resMock.Setup(r => r.GetHeight()).Returns(1080);

            var pipeline = new WallpaperPipeline(
                _geoMock.Object,
                _weatherMock.Object,
                _paletteMock.Object,
                factory,
                _setterMock.Object,
                _resMock.Object,
                _historyMock.Object,
                _settingsMock.Object,
                _loggerMock.Object);

            var result = await pipeline.RunAsync();

            Assert.True(result);
            _setterMock.Verify(s => s.Set(tempOutputPath), Times.Once);
            _historyMock.Verify(h => h.AddAsync(It.IsAny<HistoryEntry>(), 20, It.IsAny<CancellationToken>()), Times.Once);
        }
        finally
        {
            if (File.Exists(tempOutputPath))
                File.Delete(tempOutputPath);
        }
    }
}
