using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace TodayWallpaper.Core.Wallpaper;

/// <summary>
/// Sets the Windows desktop wallpaper via <c>SystemParametersInfo</c> P/Invoke.
/// The image must be a JPEG or BMP for maximum Windows compatibility.
/// </summary>
public sealed class WallpaperSetter(ILogger<WallpaperSetter> logger) : IWallpaperSetter
{
    private const int    SPI_SETDESKWALLPAPER = 20;
    private const int    SPIF_UPDATEINIFILE   = 0x01;
    private const int    SPIF_SENDCHANGE      = 0x02;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int SystemParametersInfo(
        int uAction, int uParam, string lpvParam, int fuWinIni);

    /// <summary>Absolute path where the current wallpaper JPEG is saved.</summary>
    public static readonly string CurrentWallpaperPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TodayWallpaper",
        "current_wallpaper.jpg");

    /// <inheritdoc/>
    public void Set(string absoluteImagePath)
    {
        if (!File.Exists(absoluteImagePath))
            throw new FileNotFoundException("Wallpaper image not found.", absoluteImagePath);

        logger.LogInformation("Setting desktop wallpaper: {Path}", absoluteImagePath);

        int result = SystemParametersInfo(
            SPI_SETDESKWALLPAPER,
            0,
            absoluteImagePath,
            SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);

        if (result == 0)
            logger.LogWarning("SystemParametersInfo returned 0 — wallpaper may not have been applied.");
    }
}
