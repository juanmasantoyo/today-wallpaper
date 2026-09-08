---
name: weather-layer
description: >-
  Skill for implementing the weather data fetching and the climate-to-palette
  mapping logic in TodayWallpaper. Activate when working on anything in
  TodayWallpaper.Core/Weather/ or TodayWallpaper.Core/Palette/.
---

# Skill: Weather Layer

## Responsibility

This skill covers two sub-systems:
1. **Weather fetching**: obtaining today's weather data for the user's location.
2. **Palette mapping**: translating weather conditions into a color palette.

---

## 1. Weather API: Open-Meteo (preferred)

Open-Meteo is the preferred API. It is free, requires no API key, and has no rate limits.

- **Endpoint**: `https://api.open-meteo.com/v1/forecast`
- **Docs**: https://open-meteo.com/en/docs

### Key query parameters

Fetch ALL of the following to maximize randomness entropy:

```
latitude={lat}&longitude={lon}
&current=temperature_2m,weathercode,windspeed_10m,
         relativehumidity_2m,precipitation,cloudcover,
         visibility,apparent_temperature,dewpoint_2m
&forecast_days=1
```

### WMO Weather Code to Condition mapping

| WMO Code Range | Condition     |
| -------------- | ------------- |
| 0              | Sunny         |
| 1, 2, 3        | Cloudy        |
| 45, 48         | Foggy         |
| 51-67          | Rainy         |
| 71-77          | Snowy         |
| 80-82          | Rainy (showers) |
| 85, 86         | Snowy (showers) |
| 95-99          | Stormy        |

### C# HTTP call pattern

Use IHttpClientFactory. Never create raw HttpClient instances.

```csharp
var response = await _httpClient.GetFromJsonAsync<OpenMeteoResponse>(
    $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lon}&current=weathercode,temperature_2m&forecast_days=1");
```

---

## 2. IP Geolocation

- Use `http://ip-api.com/json` (free, no key required).
- Returns JSON with `lat`, `lon`, `city`, `country`.
- Cache result in AppSettings. Do not call on every run.

---

## 3. Palette Mapping

The `IPaletteMapper` interface maps a `WeatherCondition` + `PaletteMode` to a `ColorPalette`.

```csharp
public interface IPaletteMapper
{
    ColorPalette Map(WeatherCondition condition, PaletteMode mode);
}

public record ColorPalette(
    IReadOnlyList<SKColor> PrimaryColors,
    IReadOnlyList<SKColor> SecondaryColors,
    float Temperature   // 0.0 = cold, 1.0 = warm (used for blending bias)
);
```

Colors are defined as SkiaSharp `SKColor` values.

### Contrast mode

Simply invert the palette. If Empathic returns blues/greens for Rain,
Contrast returns the Sunny palette (yellows/oranges) for Rain.

---

## 4. Data Models

```csharp
public enum WeatherCondition { Sunny, Cloudy, Rainy, Stormy, Snowy, Foggy }
public enum PaletteMode { Empathic, Contrast }

/// <summary>All parameters fetched from the weather API for a given day.</summary>
public record WeatherData(
    WeatherCondition Condition,
    int WmoCode,                    // exact WMO code (0-99)
    double TemperatureCelsius,
    double ApparentTemperatureCelsius,
    double WindspeedKmh,
    double Humidity,                // relative humidity %
    double PrecipitationMm,         // total precipitation mm
    double CloudCoverPercent,       // 0-100
    double VisibilityKm,
    double DewpointCelsius,
    string LocationName
);
```

---

## 5. Palette Configuration

Palettes are loaded from `%APPDATA%\TodayWallpaper\palettes.json`.
Ship a default file and copy it on first run if missing.

```csharp
public interface IPaletteConfigLoader
{
    PaletteConfig Load();   // returns defaults + validates all colors
}

public record PaletteConfig(
    Dictionary<WeatherCondition, PaletteEntry> Empathic,
    Dictionary<WeatherCondition, PaletteEntry> Contrast
);

public record PaletteEntry(
    IReadOnlyList<SKColor> Primary,
    IReadOnlyList<SKColor> Secondary
);
```

### Luminance Validator

After loading, validate every color. Reject (log warning + use default) if Lightness > 55%:

```csharp
public static bool IsAcceptable(SKColor color)
{
    color.ToHsl(out _, out _, out float lightness);
    return lightness <= 55f;  // HSL lightness 0-100
}
```

This ensures wallpapers always provide sufficient contrast for desktop icons and text.
