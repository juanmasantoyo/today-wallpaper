namespace TodayWallpaper.Core.Palette;

/// <summary>Loads, saves, and validates the palette configuration from disk.</summary>
public interface IPaletteConfigLoader
{
    /// <summary>
    /// Loads <see cref="PaletteConfig"/> from <c>%APPDATA%\TodayWallpaper\palettes.json</c>.
    /// Falls back to built-in defaults if the file is missing, malformed, or contains invalid colors.
    /// </summary>
    PaletteConfig Load();

    /// <summary>
    /// Persists <see cref="PaletteConfig"/> to <c>%APPDATA%\TodayWallpaper\palettes.json</c>.
    /// </summary>
    void Save(PaletteConfig config);

    /// <summary>
    /// Resets the palette configuration file back to built-in defaults.
    /// </summary>
    void ResetToDefaults();

    /// <summary>
    /// Returns the built-in default <see cref="PaletteConfig"/>.
    /// </summary>
    PaletteConfig GetDefaultConfig();
}

