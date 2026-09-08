# Today Wallpaper — Project Rules

These rules are always active and guide all development decisions for this project.

## Project Overview

**Today Wallpaper** is a Windows 11 desktop application that automatically changes
the desktop wallpaper on a configurable schedule (default: every hour). The wallpaper
is procedurally generated using abstract visual styles, and the color palette is driven
by the current weather data. The user retains full control and can pause, restore, or
pin any wallpaper from a rolling history.

---

## Technology Stack

| Layer             | Technology                                      |
| ----------------- | ----------------------------------------------- |
| UI Framework      | **WPF** (.NET 8+)                               |
| Language          | **C#** (latest stable)                          |
| Packaging         | **MSIX** via `dotnet publish`                   |
| Distribution      | **Microsoft Store**                             |
| Weather API       | TBD (Open-Meteo preferred as first candidate)   |
| Image Generation  | **System.Drawing** / **SkiaSharp**              |
| Scheduling        | **Windows Task Scheduler** (via `TaskService`)  |

---

## Project Architecture

The solution follows a clean layered architecture:

```
TodayWallpaper/
├── TodayWallpaper.App/          <- WPF application (UI + tray icon)
├── TodayWallpaper.Core/         <- Business logic (no WPF dependency)
│   ├── Weather/                 <- Weather fetching & data models
│   ├── Palette/                 <- Climate -> color palette mapping
│   ├── Generators/              <- Abstract wallpaper style generators
│   ├── Wallpaper/               <- Applying the image as desktop background
│   └── History/                 <- Wallpaper history ring buffer & pinning
├── TodayWallpaper.Worker/       <- Console entry point (Task Scheduler target)
└── TodayWallpaper.Tests/        <- xUnit tests (min. 90% coverage on Core + Worker)
```

- **TodayWallpaper.Core** must have zero WPF dependencies. It is reusable and testable in isolation.
- **TodayWallpaper.Worker** is the executable that Windows Task Scheduler calls on the configured
  interval (default: hourly). It generates a new wallpaper and writes it to the history store.
- **TodayWallpaper.App** is the tray-icon WPF app for user configuration and history browsing.

---

## Coding Conventions

- Use C# 12 features (primary constructors, collection expressions) where appropriate.
- Use async/await throughout. No blocking calls on the UI thread.
- Use ILogger<T> from Microsoft.Extensions.Logging for all logging.
- Use IOptions<T> from Microsoft.Extensions.Options for configuration.
- Prefer dependency injection via Microsoft.Extensions.DependencyInjection.
- All public types must have XML doc comments.
- File names must match their primary class name exactly.

---

## Location Strategy

- **Primary**: IP-based geolocation (automatic, no user permission required).
  - Use a free IP geolocation API (e.g., ip-api.com) to resolve lat/lon on first run.
- **Override**: User can manually set city or coordinates in settings.
- Store resolved location in user settings so it is not re-fetched every day.

---

## Weather to Palette Mapping

Two configurable modes:

| Mode          | Description                                          |
| ------------- | ---------------------------------------------------- |
| Empathic      | Colors reflect the actual weather (rain -> blues)    |
| Contrast      | Colors contrast the weather to boost mood (rain -> warm tones) |

### Palette Configuration File

Color palettes are **not hardcoded**. They are loaded from:

```
%APPDATA%\TodayWallpaper\palettes.json
```

A default `palettes.json` is shipped with the app and copied on first run.
The user can edit this file to fully customize the colors for each condition and mode.
The app must validate the file on load and fall back to defaults if malformed.

Structure (abbreviated):

```json
{
  "empathic": {
    "Sunny":  { "primary": ["#D4A017", "#E07B00", "#C85A00"], "secondary": ["#8B1A00", "#6B3A00"] },
    "Cloudy": { "primary": ["#4A5568", "#2D3748", "#5A6B7D"], "secondary": ["#3D2E5E", "#4A3D2E"] },
    "Rainy":  { "primary": ["#1A3A5C", "#1A4D3A", "#1A3D4D"], "secondary": ["#1A2A3D", "#0D1F33"] },
    "Stormy": { "primary": ["#1A0D2E", "#0D0D1F", "#1A1A2E"], "secondary": ["#003333", "#001A1A"] },
    "Snowy":  { "primary": ["#1A2A3D", "#1A3D5C", "#2A3D5C"], "secondary": ["#3D3D5C", "#2A2A4D"] },
    "Foggy":  { "primary": ["#2A2A3D", "#3D2A3D", "#2A3D3D"], "secondary": ["#1A1A2A", "#2A1A2A"] }
  },
  "contrast": { ... }
}
```

> **Color constraint**: All colors in palettes.json must have HSL Lightness <= 55%.
> Colors that are too light (L > 55%) degrade contrast with desktop icons and text.
> The validator rejects any color above this threshold.

Mapping reference (Empathic mode defaults — all dark/mid-toned):

| Condition      | Primary Colors              | Secondary Colors        |
| -------------- | --------------------------- | ----------------------- |
| Sunny / Hot    | Deep golds, burnt oranges   | Dark reds, dark ambers  |
| Cloudy         | Dark slate blues, charcoals | Deep purples, dark taupe|
| Rainy          | Dark blues, deep teals      | Dark indigo, dark slate |
| Stormy         | Near-black purples, dark navy | Very dark cyan accents |
| Snowy / Cold   | Dark steel blues, mid greys | Dark muted silvers      |
| Foggy / Misty  | Dark charcoals, dark mauves | Dark olive, dark slate  |

### Color Jitter — No Direct Mapping

Colors from the palette are **never used as-is**. Before every generation, each color
receives a randomized HSL jitter driven by the weather seed:

```csharp
/// <summary>
/// Applies bounded random jitter in HSL space to a palette color.
/// Lightness is clamped post-jitter to enforce the <= 55% constraint.
/// </summary>
SKColor ApplyJitter(SKColor baseColor, Random rng, WeatherData data)
{
    // Jitter ranges are weather-modulated, not fixed:
    float hueJitter        = rng.NextSingle() * 30f - 15f;   // +-15 deg, wider on stormy days
    float saturationJitter = rng.NextSingle() * 20f - 10f;   // +-10%
    float lightnessJitter  = rng.NextSingle() * 14f - 7f;    // +-7%, clamped to [10, 55]

    // Wind amplifies hue jitter (gusty days = more color variety)
    hueJitter *= 1f + (float)(data.WindspeedKmh / 80.0);

    baseColor.ToHsl(out float h, out float s, out float l);
    h = (h + hueJitter + 360f) % 360f;
    s = Math.Clamp(s + saturationJitter, 20f, 90f);
    l = Math.Clamp(l + lightnessJitter, 10f, 55f);
    return SKColor.FromHsl(h, s, l);
}
```

This means palette entries in `palettes.json` define a **color neighborhood**, not exact colors.
Two generations with the same condition will always look related but never identical.

---

## Randomness and Uniqueness

Every day's wallpaper must feel unique. The random seed is **derived deterministically
from the full weather data**, so the same weather on the same day always produces the
same wallpaper (reproducible), but no two real days produce the same output.

### Seed Construction

The seed includes the **current hour** so that each hourly refresh produces a distinct
image even when weather conditions have not changed.

```csharp
// Combine all available weather parameters + time into a single seed
int seed = HashCode.Combine(
    data.Condition,            // WMO weather code bucket
    data.WmoCode,              // exact WMO code for finer variation
    (int)data.TemperatureCelsius,
    (int)data.WindspeedKmh,
    (int)data.Humidity,        // relative humidity %
    (int)data.PrecipitationMm, // precipitation amount
    (int)data.CloudCoverPercent,
    (int)data.VisibilityKm,
    DateTime.Now.DayOfYear * 100 + DateTime.Now.Hour  // unique per hour
);
var rng = new Random(seed);
```

### Parameters to Fetch from Open-Meteo

Fetch all of these in the single API call:

```
current=temperature_2m,weathercode,windspeed_10m,
         relativehumidity_2m,precipitation,cloudcover,
         visibility,apparent_temperature,dewpoint_2m
```

### How Parameters Influence the Output

| Parameter             | Influence                                                        |
| --------------------- | ---------------------------------------------------------------- |
| `temperature_2m`      | Color warmth bias (warmer = slightly more red/orange shift)      |
| `windspeed_10m`       | Number and size of blobs/shapes (more wind = more shapes, smaller) |
| `relativehumidity_2m` | Blur sigma (high humidity = more blur/softness)                 |
| `precipitation`       | Opacity of secondary layer (more rain = denser overlay)         |
| `cloudcover`          | Brightness cap (overcast = darken overall composition by up to 20%) |
| `visibility`          | Noise intensity (low visibility = more Perlin noise grain)      |
| `apparent_temperature`| Hue rotation offset applied to the palette (+-15 degrees HSL)   |
| `dewpoint_2m`         | Gradient smoothness (high dew = softer transitions)             |
| `wmoCode` (exact)     | Sub-condition fine-tuning within the same bucket                |

---

## Wallpaper Generator Styles

Each generator implements IWallpaperGenerator. Available styles:

| Style ID         | Description                                                   |
| ---------------- | ------------------------------------------------------------- |
| BlurBlobs        | Overlapping gaussian-blurred colored blobs                    |
| LowPoly          | Delaunay triangulation low-poly gradient mesh                 |
| PerlinFlow       | Perlin/Simplex noise fluid field                              |
| RadialGradient   | Layered radial gradients with soft overlaps                   |
| GeometricShapes  | Translucent overlapping geometric primitives                  |

The active style can be fixed by the user or rotated randomly each day.

---

## Settings Persistence

- Settings stored in `%APPDATA%\TodayWallpaper\settings.json`.
- Schema managed via a strongly-typed `AppSettings` class.
- Never store API keys in settings if using keyless APIs (like Open-Meteo).

Key settings fields:

| Key                  | Type     | Default   | Description                                      |
| -------------------- | -------- | --------- | ------------------------------------------------ |
| `refreshIntervalHours` | int    | 1         | How often the wallpaper is regenerated (1-24)    |
| `paletteMode`        | enum     | Empathic  | Empathic or Contrast                             |
| `generatorStyle`     | string   | Random    | Fixed style ID or "Random"                       |
| `isEnabled`          | bool     | true      | Master switch — false = wallpaper control paused |
| `location`           | object   | null      | Manual lat/lon/city override (null = auto IP)    |
| `historyMaxCount`    | int      | 50        | Max wallpapers to keep in history                |

---

## Wallpaper History

The app maintains a rolling history of generated wallpapers.

- **Storage**: `%APPDATA%\TodayWallpaper\history\`
- **Format**: JPEG files named `{timestamp}_{condition}.jpg` + a `history.json` index.
- **Maximum**: 50 entries (configurable via `historyMaxCount`). When full, the oldest
  **unpinned** wallpaper is deleted. Pinned wallpapers are never auto-deleted.
- **Pinning**: The user can mark any wallpaper as pinned ("kept forever") from the tray UI.
  Pinned entries have `pinned: true` in `history.json`.
- **Restore**: Clicking any history entry sets it as the current desktop wallpaper
  without generating a new one.

```json
// history.json entry shape
{
  "id": "2026-09-04T08:00:00",
  "file": "history/20260904_0800_Rainy.jpg",
  "condition": "Rainy",
  "temperature": 14.2,
  "style": "BlurBlobs",
  "pinned": false
}
```

---

## User Control Recovery

The user must always be able to regain control of their desktop wallpaper.

- **Tray menu** exposes a **"Pause / Resume"** toggle that sets `isEnabled = false/true`.
  When paused: the Task Scheduler trigger is not removed but the Worker exits immediately
  on launch without changing the wallpaper.
- **"Restore my wallpaper"** option in the tray menu opens a Windows file picker
  pre-filtered to images, allowing the user to set any custom wallpaper.
  This also sets `isEnabled = false` automatically.
- **Uninstall**: The MSIX uninstall removes the Task Scheduler task and clears
  `%APPDATA%\TodayWallpaper\` (with user confirmation for the history folder).

---

## Scheduling

- The Worker is registered as a **Windows Task Scheduler** task triggered every N hours
  (default: 1 hour), configurable via the tray app.
- When the user changes `refreshIntervalHours` in settings, the tray app re-registers
  the Task Scheduler task with the new interval.
- The Task only fires if `isEnabled = true` (checked at Worker startup).

---

## Non-Goals for v1.0

- No multi-monitor support (single primary monitor only).
- No cloud sync of settings or history.
- No animated / video wallpapers.
