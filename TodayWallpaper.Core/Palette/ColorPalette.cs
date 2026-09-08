using SkiaSharp;

namespace TodayWallpaper.Core.Palette;

/// <summary>A fully resolved, jitter-applied color palette ready for use in a wallpaper generator.</summary>
/// <param name="PrimaryColors">Dominant colors for large fills and backgrounds.</param>
/// <param name="SecondaryColors">Accent colors for overlays and highlights.</param>
/// <param name="Temperature">
/// Warm/cold bias in [0, 1]. 0 = icy cold, 1 = tropical warm.
/// Generators may use this to nudge blend direction.
/// </param>
public record ColorPalette(
    IReadOnlyList<SKColor> PrimaryColors,
    IReadOnlyList<SKColor> SecondaryColors,
    float Temperature);
