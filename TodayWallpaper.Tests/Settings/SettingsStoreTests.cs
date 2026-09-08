using Microsoft.Extensions.Logging;
using Moq;
using TodayWallpaper.Core.Palette;
using TodayWallpaper.Core.Settings;
using Xunit;

namespace TodayWallpaper.Tests.Settings;

public class SettingsStoreTests
{
    private readonly Mock<ILogger<SettingsStore>> _loggerMock = new();

    [Fact]
    public async Task LoadAsync_FileDoesNotExist_ReturnsDefaults()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"settings_{Guid.NewGuid():N}.json");
        var store = new SettingsStore(tempFile, _loggerMock.Object);

        var settings = await store.LoadAsync();

        Assert.NotNull(settings);
        Assert.True(settings.IsEnabled);
        Assert.Equal(5, settings.SelectedGeneratorStyles.Count);
        Assert.Equal(PaletteMode.Empathic, settings.PaletteMode);
        Assert.Equal(50, settings.HistoryMaxCount);
    }

    [Fact]
    public async Task LoadAsync_CorruptedFile_ReturnsDefaults()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"settings_{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(tempFile, "{ broken json content");
        try
        {
            var store = new SettingsStore(tempFile, _loggerMock.Object);
            var settings = await store.LoadAsync();
            Assert.NotNull(settings);
            Assert.True(settings.IsEnabled);
            Assert.Equal(5, settings.SelectedGeneratorStyles.Count);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTripsCorrectly()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"settings_{Guid.NewGuid():N}.json");
        try
        {
            var store = new SettingsStore(tempFile, _loggerMock.Object);

            var customSettings = new AppSettings
            {
                IsEnabled = false,
                SelectedGeneratorStyles = new[] { "LowPoly", "PerlinFlow" },
                PaletteMode = PaletteMode.Contrast,
                HistoryMaxCount = 24,
                Location = new LocationSettings(35.6762, 139.6503, "Tokyo")
            };

            await store.SaveAsync(customSettings);
            Assert.True(File.Exists(tempFile));

            var loaded = await store.LoadAsync();
            Assert.False(loaded.IsEnabled);
            Assert.Equal(2, loaded.SelectedGeneratorStyles.Count);
            Assert.Contains("LowPoly", loaded.SelectedGeneratorStyles);
            Assert.Contains("PerlinFlow", loaded.SelectedGeneratorStyles);
            Assert.Equal(PaletteMode.Contrast, loaded.PaletteMode);
            Assert.Equal(24, loaded.HistoryMaxCount);
            Assert.NotNull(loaded.Location);
            Assert.Equal("Tokyo", loaded.Location.City);
            Assert.Equal(35.6762, loaded.Location.Latitude);
            Assert.Equal(139.6503, loaded.Location.Longitude);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}
