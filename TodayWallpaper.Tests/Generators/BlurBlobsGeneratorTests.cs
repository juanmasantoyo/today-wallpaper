using Microsoft.Extensions.Logging;
using Moq;
using SkiaSharp;
using TodayWallpaper.Core.Generators;
using TodayWallpaper.Core.Palette;
using TodayWallpaper.Core.Weather;
using Xunit;

namespace TodayWallpaper.Tests.Generators;

public class BlurBlobsGeneratorTests
{
    private readonly Mock<ILogger<BlurBlobsGenerator>> _loggerMock = new();

    private static WeatherData CreateSampleWeather() =>
        new(
            Condition: WeatherCondition.Sunny,
            WmoCode: 0,
            TemperatureCelsius: 22,
            ApparentTemperatureCelsius: 22,
            WindspeedKmh: 15,
            Humidity: 60,
            PrecipitationMm: 2,
            CloudCoverPercent: 20,
            VisibilityKm: 10,
            DewpointCelsius: 12,
            LocationName: "City");

    private static ColorPalette CreateSamplePalette() =>
        new(
            new[] { new SKColor(20, 40, 80), new SKColor(40, 70, 110) },
            new[] { new SKColor(80, 110, 150) },
            0.6f);

    [Fact]
    public async Task GenerateAsync_CreatesValidImage()
    {
        var generator = new BlurBlobsGenerator(_loggerMock.Object);
        var tempFile = Path.Combine(Path.GetTempPath(), $"test_blobs_{Guid.NewGuid():N}.jpg");

        try
        {
            var resultPath = await generator.GenerateAsync(
                CreateSamplePalette(),
                CreateSampleWeather(),
                160,
                90,
                tempFile);

            Assert.Equal(tempFile, resultPath);
            Assert.True(File.Exists(tempFile));
            var fileInfo = new FileInfo(tempFile);
            Assert.True(fileInfo.Length > 0);

            using var bitmap = SKBitmap.Decode(tempFile);
            Assert.NotNull(bitmap);
            Assert.Equal(160, bitmap.Width);
            Assert.Equal(90, bitmap.Height);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}
