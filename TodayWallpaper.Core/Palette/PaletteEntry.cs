using SkiaSharp;

namespace TodayWallpaper.Core.Palette;

/// <summary>A raw palette entry loaded from <c>palettes.json</c> before jitter is applied.</summary>
/// <param name="Name">Display name of this palette configuration.</param>
/// <param name="IsEnabled">Whether this palette is enabled for random wallpaper generation.</param>
/// <param name="Primary">Base primary colors defining the dominant hue neighbourhood.</param>
/// <param name="Secondary">Base accent colors used for overlays and secondary fills.</param>
public record PaletteEntry(
    string Name,
    bool IsEnabled,
    IReadOnlyList<SKColor> Primary,
    IReadOnlyList<SKColor> Secondary)
{
    /// <summary>Convenience constructor defaulting Name to "Default" and IsEnabled to true.</summary>
    public PaletteEntry(IReadOnlyList<SKColor> primary, IReadOnlyList<SKColor> secondary)
        : this("Default", true, primary, secondary)
    {
    }
}
