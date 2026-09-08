# TodayWallpaper Architecture 🏗️

This document describes the internal architectural structure, project separation of concerns, and pipeline data flow in **TodayWallpaper**.

---

## 🏛️ Solution Structure

The solution follows a clean, decoupled architecture organized into specialized projects:

```
today-wallpaper/
├── TodayWallpaper.Core/        # Core library (.NET 10, cross-platform capable, zero UI dependencies)
│   ├── Weather/                # Open-Meteo client, IP geolocation, and WMO mapping
│   ├── Palette/                # Palette models, HSL jitter, and strict luminance validation
│   ├── Generators/             # SkiaSharp-based procedural rendering engines
│   ├── Wallpaper/              # Win32 P/Invoke abstractions & implementation (SystemParametersInfo)
│   ├── History/                # Ring buffer management & wallpaper indexing in %APPDATA%
│   └── Settings/               # User configuration serialization and storage
│
├── TodayWallpaper.Worker/      # Lightweight console worker (.NET 10 for Windows)
│   ├── WallpaperPipeline.cs    # Pipeline orchestrator: weather → palette → render → wallpaper
│   └── TaskSchedulerRegistration.cs # Windows Task Scheduler registration & triggers
│
├── TodayWallpaper.App/         # System Tray resident app (.NET 10 WPF)
│   ├── TrayIconService.cs      # NotifyIcon management and context menu handling
│   ├── ViewModels/             # ViewModels for History and Settings windows
│   └── Views/                  # WPF Views for History and Settings
│
├── TodayWallpaper.Sandbox/     # Interactive developer calibration tool (WPF)
│   ├── SandboxViewModel.cs     # Real-time weather simulation with debounce & presets
│   └── SandboxWindow.xaml      # Live inspection UI with SkiaSharp preview
│
└── TodayWallpaper.Tests/       # Automated test suite (xUnit, Moq, Coverlet)
```

---

## 🔄 Wallpaper Pipeline Data Flow

The core workflow is encapsulated in `WallpaperPipeline` within `TodayWallpaper.Worker`:

```mermaid
graph TD
    A[Trigger: Task Scheduler or Tray Menu] --> B[Resolve Coordinates]
    B -->|Auto Mode: IP-API / Manual Mode: Config| C[Query Open-Meteo API]
    C --> D[Construct WeatherSeed]
    D --> E[Select & Map Palette]
    E --> F[HSL Jitter & Luminance Validation (20% <= L <= 55%)]
    F --> G[Instantiate Procedural Generator]
    G -->|SkiaSharp Render| H[JPEG File in %APPDATA%]
    H --> I[P/Invoke: Win32 SystemParametersInfo]
    H --> J[Update History Ring Buffer]
```

### 1. Weather Data & Deterministic Seed
1. Determines current latitude and longitude (via IP geolocation or user-configured coordinates).
2. Queries the free **Open-Meteo** API for temperature, humidity, wind speed, cloud cover, precipitation, and WMO weather code.
3. Constructs an immutable `WeatherSeed` containing normalized meteorological values.

### 2. Color Palettes & Luminance Validation
1. Looks up the corresponding weather and time-of-day palette from `palettes.json`.
2. Applies bounded pseudo-random HSL jitter so consecutive runs under similar weather yield harmonized, distinct color variations.
3. **Critical Constraint**: `LuminanceValidator` ensures all final colors fall within the **20% to 55%** lightness range ($0.20 \le L \le 0.55$). This prevents muddy/overly dark backgrounds (especially in storm conditions) as well as washed-out bright tones, maintaining optimal readability for white Windows desktop icons and fonts.

### 3. SkiaSharp Rendering
1. `WallpaperGeneratorFactory` creates the selected generator instance (`BlurBlobs`, `LowPoly`, `PerlinFlow`, `RadialGradient`, or `GeometricShapes`).
2. Draws onto an `SKSurface` canvas matching the native resolution of the primary display.
3. Encodes and writes the result to `%APPDATA%\TodayWallpaper\current_wallpaper.jpg`.

### 4. Windows Desktop Application
- Uses native Windows Win32 P/Invoke API:
  ```csharp
  SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, filePath, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
  ```

---

## 💾 Storage & Persistence

All persistent data resides in the user's local application data folder:
`%APPDATA%\TodayWallpaper\`

- **`settings.json`**: User preferences (active style, refresh interval, location mode).
- **`palettes.json`**: Weather-to-color mapping definitions.
- **`current_wallpaper.jpg`**: The active desktop wallpaper image.
- **`history/`**:
  - `history.json`: Index of generated wallpapers with metadata (timestamp, weather condition, pin state).
  - `{timestamp}_{condition}.jpg`: Stored images managed within the ring buffer (configurable max size).

---

## 🛡️ Core Design Principles
- **UI-Agnostic Core**: `TodayWallpaper.Core` can run in CLI tools, background services, or web environments with zero WPF or Windows Forms dependencies.
- **High Testability**: External boundaries (HTTP, P/Invoke, disk I/O) are abstracted behind interfaces (`IWeatherClient`, `IWallpaperSetter`, `IHistoryManager`).
- **Minimal Resource Footprint**: The tray application stays idle with negligible memory consumption, and the Worker process only runs briefly on schedule.
