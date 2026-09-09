# TodayWallpaper User Guide 📖

Welcome to the **TodayWallpaper** user guide. This document explains how to use the application day-to-day, configure its behavior, and tailor visual styles to your preferences.

---

## 🖥️ Day-to-Day Usage: System Tray

TodayWallpaper is designed to run quietly in the background. Once started, you will find its icon in the Windows taskbar notification area (next to the clock).

### Context Menu (Right-click the icon):
- **Regenerate Now**: Fetches current weather and renders a fresh wallpaper immediately.
- **Pause / Resume**: Temporarily suspends automated wallpaper refreshes (useful if you want to keep the current wallpaper).
- **History**: Opens the gallery of recently generated wallpapers.
- **Settings**: Opens the user preferences window.
- **Exit**: Closes the system tray application.

---

## ⚙️ Settings Window

Opening **Settings** lets you customize the following options:

### 1. Visual Generator Style
Choose which abstract art style appears on your desktop (you can select one or multiple to pick randomly):
- **BlurBlobs**: Soft Gaussian blobs modulated by ambient humidity.
- **LowPoly**: Delaunay-triangulated geometric mesh with lighting modulated by temperature.
- **RadialGradient**: Multi-layered harmonic gradients evoking atmospheric temperature.
- **AuroraWaves**: Undulating fluid ribbons inspired by northern lights and wind currents.
- **VoronoiMosaic**: Crystalline Voronoi cell facets with directional ambient light.
- **AtmosphericRidges**: Minimalist mountain silhouettes with atmospheric depth fog.

### 2. Location Mode
- **Automatic (Recommended)**: Resolves your general city location using anonymous IP geolocation (no GPS permissions required).
- **Manual**: Specify fixed latitude and longitude coordinates (ideal when using a VPN or drawing inspiration from another city).

### 3. Refresh Frequency
Choose the automatic update interval (default: every 1 hour).

---

## 🖼️ Managing History and Pinned Wallpapers (Pins)

TodayWallpaper automatically maintains your recent wallpapers using a **ring buffer**:

1. **Reapply a previous wallpaper**: Browse through recent creations and click **"Apply as Wallpaper"** to restore any favorite image.
2. **Pin favorites (Pin 📌)**:
   - Older wallpapers are automatically pruned as the buffer reaches capacity.
   - Marking a wallpaper with a **Pin** protects it permanently from cleanup rotations.

---

## 🎨 Advanced Customization (For Power Users)

All application data is stored in the user directory:
`%APPDATA%\TodayWallpaper\` *(paste this path directly into Windows Explorer)*.

### Customizing Colors (`palettes.json`)
You can edit `palettes.json` with any text editor to adjust color palettes for different weather conditions (clear, rain, snow, etc.):

```json
{
  "ClearSky": {
    "Primary": "#1A365D",
    "Secondary": "#2A4365",
    "Accents": ["#2B6CB0", "#3182CE"]
  }
}
```

> [!NOTE]
> **Automatic Contrast Enforcement**:
> Regardless of the palette colors configured, TodayWallpaper automatically balances brightness so that HSL luminance stays within safe thresholds ($\le 55\%$). This ensures desktop icons and labels always remain readable.

---

## ❓ Frequently Asked Questions (FAQ)

#### Why do wallpapers tend to have a mid-to-dark tone?
To ensure white Windows 11 desktop text and icon labels are consistently legible without requiring intrusive system drop-shadows or text background plates.

#### What happens if I lose my internet connection?
If the Open-Meteo query fails or the device is offline, TodayWallpaper seamlessly falls back to cached meteorological data or default seed values, ensuring wallpaper generation never crashes.

#### Does it consume significant battery or memory?
Practically none. The tray app uses only a few megabytes of RAM while idling. SkiaSharp renders each wallpaper in under 200 milliseconds once per hour and immediately releases graphics resources.
