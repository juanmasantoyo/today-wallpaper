namespace TodayWallpaper.Core.Wallpaper;

/// <summary>Sets an image as the Windows desktop wallpaper.</summary>
public interface IWallpaperSetter
{
    /// <summary>Sets the image at <paramref name="absoluteImagePath"/> as the desktop wallpaper.</summary>
    void Set(string absoluteImagePath);
}
