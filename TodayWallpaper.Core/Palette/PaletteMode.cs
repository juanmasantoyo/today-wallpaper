namespace TodayWallpaper.Core.Palette;

/// <summary>Determines how weather conditions are translated into color palettes.</summary>
public enum PaletteMode
{
    /// <summary>Colors empathically reflect the weather (rainy → dark blues and teals).</summary>
    Empathic,
    /// <summary>Colors contrast the weather to improve mood (rainy → warm golds and oranges).</summary>
    Contrast
}
