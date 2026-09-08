# Contribution and Development Guide 🛠️

Thank you for your interest in contributing to **TodayWallpaper**! This document provides guidelines and instructions to set up your development environment, build the solution, run tests, and contribute effectively.

---

## 📋 Prerequisites

- **Operating System**: Windows 10 (version 19041 or higher) or Windows 11.
- **SDK**: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (v10.0.100 or higher).
- **Recommended IDE**: Visual Studio 2026, JetBrains Rider, or VS Code with the C# Dev Kit extension.
- **Workloads**: .NET Desktop Development (WPF support).

---

## 🚀 Setup & Building

### 1. Clone the repository
```powershell
git clone https://github.com/tu-usuario/today-wallpaper.git
cd today-wallpaper
```

### 2. Build the entire solution
The project uses the modern .NET solution format (`TodayWallpaper.slnx`):

```powershell
dotnet build TodayWallpaper.slnx
```

---

## 🧪 Automated Testing & Quality

The project maintains strict quality standards with **>90%** code coverage across the `Core` and `Worker` libraries.

### Run tests
```powershell
dotnet test TodayWallpaper.slnx
```

### Collect code coverage metrics
```powershell
dotnet test TodayWallpaper.slnx --collect:"XPlat Code Coverage"
```

---

## 🎨 Visual Calibration Environment: TodayWallpaper.Sandbox

To develop or fine-tune visual generators without waiting for timer cycles or modifying your actual desktop wallpaper, use the interactive **Sandbox** tool:

```powershell
dotnet run --project TodayWallpaper.Sandbox\TodayWallpaper.Sandbox.csproj
```

### Sandbox Capabilities:
- **Real-time testing**: Select any of the 5 available generators.
- **Weather presets**: Simulate weather states such as Sunny, Rain, Storm, Fog, or Snow with a single click.
- **Sliders**: Modulate temperature, wind, humidity, cloud cover, and visibility individually.
- **Interactive auto-refresh**: Automatically regenerates the artwork with a debounced delay whenever any control is adjusted.
- **Export & Apply**: Save generated images to disk or apply them immediately as your wallpaper to verify Windows icon contrast.

---

## ⚙️ Running Individual Components

### Run the System Tray application
```powershell
dotnet run --project TodayWallpaper.App\TodayWallpaper.App.csproj
```

### Run a single pass of the background orchestrator (Worker)
```powershell
dotnet run --project TodayWallpaper.Worker\TodayWallpaper.Worker.csproj
```

---

## 🧩 How to Create a New Procedural Generator

To introduce a new visual style:

1. **Implement `IWallpaperGenerator`**: Create your class in [TodayWallpaper.Core/Generators/](file:///d:/Work/Local/today-wallpaper/TodayWallpaper.Core/Generators).
2. **Use SkiaSharp**: All rendering should be drawn onto an `SKCanvas` utilizing mathematics and weather variables from the `WeatherSeed` object.
3. **Respect Luminance**: Ensure compliance with the HSL luminance constraint ($L \le 55\%$) so white desktop icons retain high contrast and readability.
4. **Register in Factory**: Add the new generator to `WallpaperGeneratorFactory.cs`.
5. **Add Unit Tests**: Write deterministic rendering and error-handling tests under `TodayWallpaper.Tests/Generators/`.

For more details on internal architecture, see [docs/architecture.md](file:///d:/Work/Local/today-wallpaper/docs/architecture.md).

---

## 📝 Code Conventions & Pull Requests

1. **C# Style**:
   - C# 13 / .NET 10 with Nullable Reference Types enabled (`#nullable enable`).
   - File-scoped namespaces (`namespace TodayWallpaper...;`).
   - Zero UI framework dependencies in `TodayWallpaper.Core`.
2. **Branches**: Use descriptive branch names (`feat/new-generator`, `fix/hsl-contrast`, etc.).
3. **Commits**: Write clear, atomic commit messages (ideally following [Conventional Commits](https://www.conventionalcommits.org/)).
4. **Pull Requests**:
   - Ensure `dotnet test` passes cleanly locally.
   - Clearly describe the purpose of the PR and attach screenshots if modifying UI or visual generators.
