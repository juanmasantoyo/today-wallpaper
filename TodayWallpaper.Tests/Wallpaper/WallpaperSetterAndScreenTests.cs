using Microsoft.Extensions.Logging;
using Moq;
using TodayWallpaper.Core.Wallpaper;
using Xunit;

namespace TodayWallpaper.Tests.Wallpaper;

public class WallpaperSetterAndScreenTests
{
    [Fact]
    public void WallpaperSetter_ThrowsFileNotFound_WhenPathInvalid()
    {
        var loggerMock = new Mock<ILogger<WallpaperSetter>>();
        var setter = new WallpaperSetter(loggerMock.Object);

        Assert.Throws<FileNotFoundException>(() =>
            setter.Set(@"C:\non_existent_folder_xyz\fake_wallpaper.jpg"));
    }

    [Fact]
    public void WallpaperSetter_Set_WithExistingFile_DoesNotThrow()
    {
        var loggerMock = new Mock<ILogger<WallpaperSetter>>();
        var setter = new WallpaperSetter(loggerMock.Object);

        var tempFile = Path.Combine(Path.GetTempPath(), $"dummy_wp_{Guid.NewGuid():N}.jpg");
        File.WriteAllBytes(tempFile, new byte[] { 0xFF, 0xD8, 0xFF });
        try
        {
            var exception = Record.Exception(() => setter.Set(tempFile));
            Assert.Null(exception);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void WallpaperSetter_CurrentWallpaperPath_IsDefined()
    {
        Assert.False(string.IsNullOrWhiteSpace(WallpaperSetter.CurrentWallpaperPath));
        Assert.EndsWith(".png", WallpaperSetter.CurrentWallpaperPath, StringComparison.OrdinalIgnoreCase);
    }

    private readonly Xunit.Abstractions.ITestOutputHelper _output;

    public WallpaperSetterAndScreenTests(Xunit.Abstractions.ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ScreenResolutionProvider_ReturnsPositiveDimensionsOnWindows()
    {
        var provider = new ScreenResolutionProvider();
        int width = provider.GetWidth();
        int height = provider.GetHeight();

        _output.WriteLine($"Detected Physical Screen Resolution: {width}x{height}");

        // On interactive Windows sessions width & height are > 0
        Assert.True(width > 0);
        Assert.True(height > 0);
    }

    [Fact]
    public void CurrentWallpaper_MatchesPhysicalResolution_IfFileExists()
    {
        var path = WallpaperSetter.CurrentWallpaperPath;
        if (!File.Exists(path)) return;

        using var codec = SkiaSharp.SKCodec.Create(path);
        if (codec is null) return;

        var provider = new ScreenResolutionProvider();
        _output.WriteLine($"Current Wallpaper image on disk: {codec.Info.Width}x{codec.Info.Height}, Expected Screen: {provider.GetWidth()}x{provider.GetHeight()}");

        Assert.Equal(provider.GetWidth(), codec.Info.Width);
        Assert.Equal(provider.GetHeight(), codec.Info.Height);
    }
}
