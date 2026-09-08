---
name: wallpaper-engine
description: >-
  Skill for implementing procedural wallpaper generation and applying the result
  as the Windows desktop background. Activate when working on anything in
  TodayWallpaper.Core/Generators/ or TodayWallpaper.Core/Wallpaper/.
---

# Skill: Wallpaper Engine

## Responsibility

This skill covers:
1. **Procedural image generation**: creating abstract wallpapers from a color palette.
2. **Desktop integration**: setting the generated image as the Windows background.

---

## 1. Image Generation Library

Use **SkiaSharp** (cross-platform, GPU-accelerated, no GDI+ limitations).

```xml
<PackageReference Include="SkiaSharp" Version="2.*" />
<PackageReference Include="SkiaSharp.NativeAssets.Win32" Version="2.*" />
```

Canvas setup:

```csharp
var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
using var surface = SKSurface.Create(info);
var canvas = surface.Canvas;
```

Save as PNG:

```csharp
using var image = surface.Snapshot();
using var data = image.Encode(SKEncodedImageFormat.Png, 100);
await File.WriteAllBytesAsync(outputPath, data.ToArray());
```

---

## 2. Generator Interface

```csharp
public interface IWallpaperGenerator
{
    string StyleId { get; }
    Task<string> GenerateAsync(ColorPalette palette, int width, int height, string outputPath);
}
```

All generators live in `TodayWallpaper.Core/Generators/`. Register all via DI:

```csharp
services.AddTransient<IWallpaperGenerator, BlurBlobsGenerator>();
services.AddTransient<IWallpaperGenerator, LowPolyGenerator>();
// etc.
```

---

## 3. Generator Implementations

### BlurBlobsGenerator
- Draw N (8-15) filled circles with colors sampled from palette.
- Positions: random within bounds.
- Radii: random between screen_height/4 and screen_height/1.5.
- Apply heavy Gaussian blur (sigma 60-120px) using `SKImageFilter.CreateBlur`.
- Layer a secondary semi-transparent blob pass for depth.

### LowPolyGenerator
- Generate M (200-500) random points across the canvas.
- Add corner/edge points to fill the frame.
- Triangulate using Delaunay triangulation (use `MathNet.Spatial` or a custom impl).
- For each triangle: sample its centroid color from a smooth gradient built from the palette.
- Fill each triangle with its color, no outline (or hairline outline at 0.3 opacity).

### PerlinFlowGenerator
- Generate a 2D Perlin/Simplex noise field.
- Map noise value (0..1) to a color along a gradient built from the palette.
- Use `noise.cs` or `FastNoiseLite` NuGet package.

### RadialGradientGenerator
- Place 3-5 radial gradient shaders at random positions.
- Each shader uses 2 colors from the palette.
- Composite them with `SKBlendMode.Screen` or `SKBlendMode.Overlay`.

### GeometricShapesGenerator
- Draw 20-50 random shapes (circles, rectangles, triangles, hexagons).
- Fill with palette colors at 30-70% opacity.
- Scale randomly. Rotate randomly.
- Apply subtle drop shadow with `SKImageFilter.CreateDropShadow`.

---

## 4. Resolution Detection

```csharp
// Get primary monitor resolution
var width = (int)SystemParameters.PrimaryScreenWidth;
var height = (int)SystemParameters.PrimaryScreenHeight;
```

In TodayWallpaper.Worker (no WPF), use:

```csharp
[DllImport("user32.dll")]
static extern int GetSystemMetrics(int nIndex);
var width = GetSystemMetrics(0);   // SM_CXSCREEN
var height = GetSystemMetrics(1);  // SM_CYSCREEN
```

---

## 5. Setting the Desktop Wallpaper

```csharp
public static class WallpaperSetter
{
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int SystemParametersInfo(
        int uAction, int uParam, string lpvParam, int fuWinIni);

    private const int SPI_SETDESKWALLPAPER = 20;
    private const int SPIF_UPDATEINIFILE   = 0x01;
    private const int SPIF_SENDCHANGE      = 0x02;

    public static void Set(string absoluteImagePath)
    {
        SystemParametersInfo(
            SPI_SETDESKWALLPAPER, 0,
            absoluteImagePath,
            SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
    }
}
```

- The image must be saved as a **BMP or JPEG** for maximum compatibility with `SystemParametersInfo`.
  PNG may not work on all Windows configurations. Save as JPEG (quality 95) as the final step.
- Save to: `%APPDATA%\TodayWallpaper\current_wallpaper.jpg`

---

## 6. Task Scheduler Registration

Register the Worker executable to run once per day at a configurable time (default 07:00):

```csharp
using Microsoft.Win32.TaskScheduler;

using var ts = new TaskService();
var td = ts.NewTask();
td.Triggers.Add(new DailyTrigger { StartBoundary = DateTime.Today.AddHours(7) });
td.Actions.Add(new ExecAction(workerExePath));
ts.RootFolder.RegisterTaskDefinition("TodayWallpaper", td);
```

NuGet: `TaskScheduler` by dahall.
