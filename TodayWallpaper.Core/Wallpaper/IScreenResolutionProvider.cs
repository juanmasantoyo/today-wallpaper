namespace TodayWallpaper.Core.Wallpaper;

/// <summary>Returns the dimensions of the primary monitor.</summary>
public interface IScreenResolutionProvider
{
    /// <summary>Gets the width of the primary screen in physical pixels.</summary>
    int GetWidth();

    /// <summary>Gets the height of the primary screen in physical pixels.</summary>
    int GetHeight();
}
