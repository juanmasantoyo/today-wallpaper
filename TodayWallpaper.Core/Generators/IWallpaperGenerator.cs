using TodayWallpaper.Core.Palette;
using TodayWallpaper.Core.Weather;

namespace TodayWallpaper.Core.Generators;

/// <summary>Generates a procedural wallpaper image from a color palette and weather data.</summary>
public interface IWallpaperGenerator
{
    /// <summary>Unique identifier for this generator style (e.g. "BlurBlobs").</summary>
    string StyleId { get; }

    /// <summary>
    /// Procedurally generates a wallpaper image and saves it to <paramref name="outputPath"/>.
    /// </summary>
    /// <param name="palette">Jitter-applied color palette.</param>
    /// <param name="weather">Raw weather data used to modulate composition parameters.</param>
    /// <param name="width">Output image width in pixels.</param>
    /// <param name="height">Output image height in pixels.</param>
    /// <param name="outputPath">Absolute path for the output JPEG file.</param>
    /// <param name="cancellationToken">Propagates cancellation.</param>
    /// <returns>The <paramref name="outputPath"/> on success.</returns>
    Task<string> GenerateAsync(
        ColorPalette palette,
        WeatherData weather,
        int width,
        int height,
        string outputPath,
        CancellationToken cancellationToken = default);
}
