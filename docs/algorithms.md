# Procedural Generation Algorithms 🎨

**TodayWallpaper** does not rely on heavy generative AI models or static web downloads. All artwork is computed **at runtime** using mathematical C# algorithms powered by the **SkiaSharp** 2D graphics engine.

---

## 🧮 How Weather Influences the Art

Each image is deterministically derived from a `WeatherSeed`. Real-world meteorological parameters modulate the following visual properties:

| Meteorological Variable | Normal Range | Effect on Generation |
| :--- | :--- | :--- |
| **Temperature** | $-20^\circ\text{C}$ to $45^\circ\text{C}$ | Color temperature balance (cool/blue vs. warm/amber/reddish tones). |
| **Cloud Cover** | $0\%$ to $100\%$ | Palette desaturation, overall contrast, and atmospheric diffusion. |
| **Relative Humidity** | $0\%$ to $100\%$ | Gaussian blur radius, vapor density, and gradient softness. |
| **Wind Speed** | $0$ to $100\text{ km/h}$ | Particle stroke length in vector flow fields, angular dispersion, and distortion. |
| **Precipitation** | $0$ to $>50\text{ mm}$ | Layer overlay density and background noise intensity. |
| **WMO Code** | WMO standard weather codes | Base thematic palette selection (Snow, Fog, Storm, Clear, etc.). |

---

## 🖌️ The 5 Procedural Generators

### 1. BlurBlobs (`BlurBlobsGenerator`)
- **Concept**: Soft, overlapping Gaussian blobs emulating condensation and ambient lighting.
- **Technique**: Elliptical disks are scattered across weighted pseudo-random coordinates. A convolutional Gaussian blur filter (`SKImageFilter.CreateBlur`) is applied with a dynamic radius driven by relative humidity.
- **Result**: Gentle, minimalist wallpapers ideal for users seeking minimal visual distraction.

### 2. LowPoly (`LowPolyGenerator`)
- **Concept**: A faceted mesh of irregular polygons with a subtle 3D relief effect.
- **Technique**:
  1. A set of noise-dispersed control points is generated.
  2. The **Bowyer-Watson algorithm** computes a true Delaunay triangulation in real time.
  3. Each triangle is filled with a gradient interpolated across its three vertices, modulated by ambient temperature and a simulated light source angle.
- **Result**: A crisp, modern, architectural aesthetic.

### 3. PerlinFlow (`PerlinFlowGenerator`)
- **Concept**: Continuous streamline vectors resembling wind currents or topographic map contours.
- **Technique**:
  1. A directional angle matrix is calculated using continuous 2D noise functions (Perlin gradient / fBm).
  2. Hundreds of virtual particles traverse the grid following the computed directional vectors.
  3. Each step draws a segment onto an `SKPath` with smooth alpha fade-out and stroke width mapped to wind speed.
- **Result**: Dynamic, organic patterns conveying natural flow and motion.

### 4. RadialGradient (`RadialGradientGenerator`)
- **Concept**: Layered harmonic spherical gradients with off-center focal points.
- **Technique**: Combines multiple color stops into elliptical radial shaders (`SKShader.CreateRadialGradient`), scaling diffusion with cloud cover and chromatic vibrancy with temperature.
- **Result**: Deep sky atmosphere with clean, ambient depth.

### 5. GeometricShapes (`GeometricShapesGenerator`)
- **Concept**: Abstract compositions based on Euclidean geometry, golden proportions, and rhythmic layering.
- **Technique**: Generates regular and irregular polygons with angular rotations driven by wind direction and alpha-blending modes (`SKBlendMode.SrcOver`).
- **Result**: High-impact contemporary abstract art.

---

## 🔒 Strict HSL Luminance Control

A core design principle of the engine is that **the wallpaper must never compromise desktop usability**.

To guarantee this, RGB colors are mapped to HSL color space prior to rasterization:
$$\text{Luminance } (L) = \frac{\max(R, G, B) + \min(R, G, B)}{2}$$

If a computed color exceeds the upper threshold:
$$L > 0.55$$
The engine automatically clamps the lightness component to a safe ceiling ($\le 55\%$) while preserving its Hue and Saturation. This guarantees optimal contrast for white desktop shortcut text and icons on Windows 11.
