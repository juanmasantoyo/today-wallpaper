using System.Text.Json;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using TodayWallpaper.Core.Weather;

namespace TodayWallpaper.Core.Palette;

/// <summary>
/// Loads the palette configuration from <c>%APPDATA%\TodayWallpaper\palettes.json</c>.
/// Validates color formats. Recommends lightness range (20%–55%) without rejecting out-of-bounds user colors.
/// Falls back to defaults when corrupted or unparseable.
/// Copies the default file on first run.
/// </summary>
public sealed class PaletteConfigLoader : IPaletteConfigLoader
{
    private static readonly string DefaultConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TodayWallpaper",
        "palettes.json");

    private readonly ILogger<PaletteConfigLoader> _logger;
    private readonly string _configPath;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions JsonWriteOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Initializes a new instance of <see cref="PaletteConfigLoader"/>.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="configPath">Optional custom file path for palettes.json.</param>
    public PaletteConfigLoader(ILogger<PaletteConfigLoader> logger, string? configPath = null)
    {
        _logger = logger;
        _configPath = configPath ?? DefaultConfigPath;
    }

    /// <inheritdoc/>
    public PaletteConfig Load()
    {
        EnsureDefaultExists();

        try
        {
            var json = File.ReadAllText(_configPath);
            var dto  = JsonSerializer.Deserialize<PaletteConfigDto>(json, JsonOptions)
                       ?? throw new InvalidOperationException("palettes.json deserialized to null.");
            return Validate(dto);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load palettes.json; using built-in defaults.");
            return DefaultPalettes.Config;
        }
    }

    /// <inheritdoc/>
    public void Save(PaletteConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        var dir = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var dto = new PaletteConfigSaveDto
        {
            Empathic = config.Empathic.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => kvp.Value.Select(e => new PaletteEntryDto
                {
                    Name = e.Name,
                    IsEnabled = e.IsEnabled,
                    Primary = e.Primary.Select(c => $"#{c.Red:X2}{c.Green:X2}{c.Blue:X2}").ToArray(),
                    Secondary = e.Secondary.Select(c => $"#{c.Red:X2}{c.Green:X2}{c.Blue:X2}").ToArray()
                }).ToList()),
            Contrast = config.Contrast.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => kvp.Value.Select(e => new PaletteEntryDto
                {
                    Name = e.Name,
                    IsEnabled = e.IsEnabled,
                    Primary = e.Primary.Select(c => $"#{c.Red:X2}{c.Green:X2}{c.Blue:X2}").ToArray(),
                    Secondary = e.Secondary.Select(c => $"#{c.Red:X2}{c.Green:X2}{c.Blue:X2}").ToArray()
                }).ToList())
        };

        var json = JsonSerializer.Serialize(dto, JsonWriteOptions);
        File.WriteAllText(_configPath, json);
        _logger.LogInformation("Saved palettes.json to {Path}", _configPath);
    }

    /// <inheritdoc/>
    public void ResetToDefaults()
    {
        var dir = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllText(_configPath, DefaultPalettes.Json);
        _logger.LogInformation("Reset palettes.json to defaults at {Path}", _configPath);
    }

    /// <inheritdoc/>
    public PaletteConfig GetDefaultConfig() => DefaultPalettes.Config;

    // ── private helpers ─────────────────────────────────────────────────────

    private void EnsureDefaultExists()
    {
        if (File.Exists(_configPath)) return;
        var dir = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(_configPath, DefaultPalettes.Json);
        _logger.LogInformation("Created default palettes.json at {Path}", _configPath);
    }

    private PaletteConfig Validate(PaletteConfigDto dto)
    {
        var empathic = ValidateSection(dto.Empathic, "empathic", DefaultPalettes.Config.Empathic);
        var contrast = ValidateSection(dto.Contrast, "contrast", DefaultPalettes.Config.Contrast);
        return new PaletteConfig(empathic, contrast);
    }

    private Dictionary<WeatherCondition, List<PaletteEntry>> ValidateSection(
        Dictionary<string, JsonElement>? section,
        string sectionName,
        Dictionary<WeatherCondition, List<PaletteEntry>> defaults)
    {
        var result = new Dictionary<WeatherCondition, List<PaletteEntry>>();
        foreach (var condition in Enum.GetValues<WeatherCondition>())
        {
            var key = condition.ToString();
            if (section is not null && section.TryGetValue(key, out var element))
            {
                var entries = ParseEntriesFromElement(element, condition, sectionName, defaults[condition]);
                result[condition] = entries.Count > 0 ? entries : [.. defaults[condition]];
            }
            else
            {
                result[condition] = [.. defaults[condition]];
            }
        }
        return result;
    }

    private List<PaletteEntry> ParseEntriesFromElement(
        JsonElement element,
        WeatherCondition condition,
        string sectionName,
        List<PaletteEntry> fallbackDefaults)
    {
        var entries = new List<PaletteEntry>();
        var defEntry = fallbackDefaults.FirstOrDefault() ?? new PaletteEntry("Default", true, [new SKColor(50, 70, 90)], [new SKColor(30, 40, 60)]);

        if (element.ValueKind == JsonValueKind.Array)
        {
            int index = 1;
            foreach (var item in element.EnumerateArray())
            {
                try
                {
                    var dto = item.Deserialize<PaletteEntryDto>(JsonOptions);
                    if (dto is not null)
                    {
                        var entry = ParseSingleEntry(dto, condition, sectionName, defEntry, $"Paleta {index}");
                        entries.Add(entry);
                        index++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse a palette entry in {Section}.{Condition}; using default.", sectionName, condition);
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Object)
        {
            try
            {
                var dto = element.Deserialize<PaletteEntryDto>(JsonOptions);
                if (dto is not null)
                {
                    entries.Add(ParseSingleEntry(dto, condition, sectionName, defEntry, "Default"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse single palette entry in {Section}.{Condition}; using default.", sectionName, condition);
            }
        }

        return entries.Count > 0 ? entries : [.. fallbackDefaults];
    }

    private PaletteEntry ParseSingleEntry(
        PaletteEntryDto dto,
        WeatherCondition condition,
        string sectionName,
        PaletteEntry defEntry,
        string defaultName)
    {
        var name = string.IsNullOrWhiteSpace(dto.Name) ? defaultName : dto.Name.Trim();
        var isEnabled = dto.IsEnabled ?? true;
        var primary = ParseColors(dto.Primary, condition, sectionName, "primary", defEntry.Primary);
        var secondary = ParseColors(dto.Secondary, condition, sectionName, "secondary", defEntry.Secondary);
        return new PaletteEntry(name, isEnabled, primary, secondary);
    }

    private IReadOnlyList<SKColor> ParseColors(
        string[]? hexColors,
        WeatherCondition condition,
        string section,
        string role,
        IReadOnlyList<SKColor> fallback)
    {
        if (hexColors is null || hexColors.Length == 0) return fallback;

        var result = new List<SKColor>(hexColors.Length);
        foreach (var hex in hexColors)
        {
            if (!SKColor.TryParse(hex, out var color))
            {
                _logger.LogWarning(
                    "Invalid color '{Hex}' in {Section}.{Condition}.{Role} — using fallback defaults.",
                    hex, section, condition, role);
                return fallback;
            }
            if (!LuminanceValidator.IsAcceptable(color))
            {
                _logger.LogInformation(
                    "Color '{Hex}' in {Section}.{Condition}.{Role} is outside recommended lightness bounds [{Min}%, {Max}%], accepted by policy.",
                    hex, section, condition, role, LuminanceValidator.MinLightness, LuminanceValidator.MaxLightness);
            }
            result.Add(color);
        }
        return result.AsReadOnly();
    }

    // ── DTOs ────────────────────────────────────────────────────────────────

    private sealed class PaletteConfigDto
    {
        public Dictionary<string, JsonElement>? Empathic { get; init; }
        public Dictionary<string, JsonElement>? Contrast { get; init; }
    }

    private sealed class PaletteConfigSaveDto
    {
        public Dictionary<string, List<PaletteEntryDto>>? Empathic { get; init; }
        public Dictionary<string, List<PaletteEntryDto>>? Contrast { get; init; }
    }

    private sealed class PaletteEntryDto
    {
        public string? Name { get; init; }
        public bool? IsEnabled { get; init; }
        public string[]? Primary   { get; init; }
        public string[]? Secondary { get; init; }
    }
}
