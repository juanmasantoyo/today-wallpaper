# TodayWallpaper ⛅🎨

**TodayWallpaper** is a modern, lightweight Windows 11 desktop application that automatically refreshes your desktop background with **100% procedurally generated abstract art**, synchronized in real-time with local weather conditions.

---

## ✨ Key Features

- **🎨 100% Mathematical & Procedural (No AI)**: Every wallpaper is rendered on the fly in milliseconds using geometric algorithms, continuous noise vector fields, and triangulated meshes powered by [SkiaSharp](https://github.com/mono/SkiaSharp).
- **🌦️ True-to-Life Weather Reflection**: Temperature, humidity, wind, cloud cover, and precipitation modulate the color palette, shape density, and rendering dynamics.
- **👁️ Always Legible Desktop Icons**: Guaranteed contrast with strict HSL luminance validation ($L \le 55\%$). Your desktop shortcuts and white Windows system typography remain crisp and readable at all times.
- **📌 History Ring Buffer with Pinning**: Browse creations from past days, reapply any previous wallpaper, or pin your favorites to keep them permanently.
- **🪟 Lightweight System Tray App**: Lives quietly in the notification area with near-zero CPU and memory usage. Force a refresh, pause updates, or adjust preferences with a single click.

---

## 🎨 Included Visual Styles

| Style | Base Algorithm | Primary Weather Influence |
| :--- | :--- | :--- |
| **BlurBlobs** | Soft Gaussian blobs | **Humidity** controls blur radius; **wind** drives dispersion. |
| **LowPoly** | Delaunay Triangulation (*Bowyer-Watson*) | **Temperature** modulates chromatic interpolation across facets. |
| **PerlinFlow** | Continuous 2D noise vector fields | **Wind speed** governs particle path length and dynamism. |
| **RadialGradient** | Harmonic concentric gradients | **Cloud cover** scales atmospheric light diffusion. |
| **GeometricShapes** | Euclidean composition & alpha blending | **Wind and pressure** rotate and distribute polygons. |

---

## 📥 Installation & Getting Started

### Option 1: Microsoft Store (Recommended)

*Coming soon to the Microsoft Store.*

### Option 2: Executable / Packaged Release

You can download the latest release from the **[Releases](https://github.com/tu-usuario/today-wallpaper/releases)** section.

Once launched, TodayWallpaper resides in your system tray (next to the clock) and schedules periodic wallpaper updates.

---

## 📚 Documentation

For in-depth guides on every part of the project, check the dedicated documentation:

| Document | Audience | Description |
| :--- | :--- | :--- |
| 📖 **[User Guide](docs/user-guide.md)** | Users / Power users | System tray usage, color customization (`palettes.json`), history management, and FAQ. |
| 🎨 **[Procedural Algorithms](docs/algorithms.md)** | Curious minds / Designers | Mathematics behind each generator, HSL chromatic transformations, and weather modulation. |
| 🛠️ **[Contribution Guide](CONTRIBUTING.md)** | Developers | Environment setup (.NET 10), building, unit tests, interactive **Sandbox** usage, and PR guidelines. |
| 🏗️ **[Solution Architecture](docs/architecture.md)** | Developers / Architects | Project structure, decoupled pipeline, Win32 P/Invoke, and Windows Task Scheduler integration. |
| 📦 **[Packaging & Publishing](docs/store-publish.md)** | Maintainers | MSIX package generation, manifest configuration, and Microsoft Store requirements. |
