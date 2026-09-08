using Microsoft.Extensions.Logging;
using SkiaSharp;
using TodayWallpaper.Core.Palette;
using TodayWallpaper.Core.Weather;

namespace TodayWallpaper.Core.Generators;

/// <summary>
/// Generates a low-poly wallpaper using Delaunay triangulation (Bowyer–Watson algorithm).
/// Point density is modulated by wind speed; colour smoothness by dew point.
/// </summary>
public sealed class LowPolyGenerator(ILogger<LowPolyGenerator> logger) : IWallpaperGenerator
{
    /// <inheritdoc/>
    public string StyleId => "LowPoly";

    /// <inheritdoc/>
    public Task<string> GenerateAsync(
        ColorPalette palette,
        WeatherData weather,
        int width,
        int height,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("LowPoly: generating {W}×{H}", width, height);
        var rng = new Random(WeatherSeed.Build(weather));

        int pointCount = (int)Math.Clamp(180 + weather.WindspeedKmh * 2, 180, 450);

        // Generate random points + border anchors.
        var points = GeneratePoints(rng, width, height, pointCount);

        // Delaunay triangulation using robust Bowyer-Watson.
        var triangles = BowyerWatson(points, width, height);

        // Render.
        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;
        canvas.Clear(palette.PrimaryColors[0]);

        // Dew-point controls colour smoothness (high dew = softer facet transitions).
        float blend = (float)Math.Clamp((weather.DewpointCelsius + 10) / 40.0, 0.1, 0.9);

        var allColors = palette.PrimaryColors.Concat(palette.SecondaryColors).ToArray();

        // Directional pseudo-lighting for faceted 3D relief (light from top-left).
        float lightAngle = MathF.PI * 0.25f;
        float lightX = MathF.Cos(lightAngle);
        float lightY = MathF.Sin(lightAngle);

        foreach (var (a, b, c) in triangles)
        {
            float cx = (a.X + b.X + c.X) / 3f;
            float cy = (a.Y + b.Y + c.Y) / 3f;

            // Calculate facet normal / pseudo-lighting based on triangle orientation
            float abx = b.X - a.X, aby = b.Y - a.Y;
            float acx = c.X - a.X, acy = c.Y - a.Y;
            float cross = abx * acy - aby * acx;

            // Approximate slant direction
            float slantX = -aby + acy;
            float slantY = abx - acx;
            float slantLen = MathF.Sqrt(slantX * slantX + slantY * slantY);
            float dot = slantLen > 1e-4f ? (slantX * lightX + slantY * lightY) / slantLen : 0f;
            if (cross < 0) dot = -dot;

            float facetShade = Math.Clamp(dot, -1f, 1f);

            var color = SampleFacetColor(allColors, cx / width, cy / height, blend, facetShade, rng);

            using var path = new SKPath();
            path.MoveTo(a);
            path.LineTo(b);
            path.LineTo(c);
            path.Close();

            using var fill = new SKPaint { IsAntialias = true, IsDither = true, Color = color };
            canvas.DrawPath(path, fill);

            // Subtle hairline stroke to accentuate geometric crystal edges
            using var edge = new SKPaint
            {
                IsAntialias = true,
                IsDither = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 0.5f,
                Color = color.WithAlpha(45)
            };
            canvas.DrawPath(path, edge);
        }

        SaveJpeg(surface, outputPath);
        return Task.FromResult(outputPath);
    }

    // ── colour sampling ──────────────────────────────────────────────────────

    private static SKColor SampleFacetColor(
        SKColor[] colors,
        float nx,
        float ny,
        float blend,
        float facetShade,
        Random rng)
    {
        // 2D spatial distribution using noise to create rich, organic chromatic patches
        float noise = NoiseHelper.Noise(nx * 2.2f + 1.3f, ny * 2.2f + 2.7f);
        float spatialT = Math.Clamp(nx * 0.55f + ny * 0.35f + (noise - 0.5f) * 0.40f, 0f, 0.9999f);

        // Modulate palette position subtly by facet orientation for 3D chromatic relief
        float paletteOffset = facetShade * 0.10f * (1f - blend * 0.3f);
        float t = Math.Clamp(spatialT + paletteOffset, 0f, 0.9999f);

        float scaled = t * (colors.Length - 1);
        int idx1 = (int)scaled;
        int idx2 = Math.Min(idx1 + 1, colors.Length - 1);
        float frac = scaled - idx1;

        // Dew-point softens micro-noise on transitions
        frac = Math.Clamp(frac + (rng.NextSingle() - 0.5f) * (1f - blend) * 0.15f, 0f, 1f);
        var baseColor = LerpHsl(colors[idx1], colors[idx2], frac);

        baseColor.ToHsl(out float h, out float s, out float l);

        // Gentle facet relief via Hue Shift (warm highlight vs cool shade) without pushing into clash hues
        float facetHueShift = facetShade * 5f * (1f - blend * 0.4f);
        float modulatedH = (h + facetHueShift + 360f) % 360f;

        // Respect palette's natural saturation and cap upper bound to prevent blinding neon spots
        float modulatedS = Math.Clamp(s, 0f, 70f);

        // Subtle, bounded luminance variation (±2.5%) preserving true color identity and desktop contrast
        float subtleL = Math.Clamp(l + facetShade * 2.5f * (1f - blend * 0.5f), 22f, 52f);

        return SKColor.FromHsl(modulatedH, modulatedS, subtleL);
    }

    private static SKColor LerpHsl(SKColor a, SKColor b, float t)
    {
        a.ToHsl(out float h1, out float s1, out float l1);
        b.ToHsl(out float h2, out float s2, out float l2);

        // Shortest circular hue arc
        float diff = (h2 - h1 + 360f) % 360f;
        if (diff > 180f) diff -= 360f;
        float h = (h1 + diff * t + 360f) % 360f;

        // Interpolate natural saturation and luminance smoothly without artificial inflation
        float s = Math.Clamp(s1 + (s2 - s1) * t, 0f, 75f);
        float l = Math.Clamp(l1 + (l2 - l1) * t, LuminanceValidator.MinLightness, LuminanceValidator.MaxLightness);

        return SKColor.FromHsl(h, s, l);
    }

    // ── point generation ─────────────────────────────────────────────────────

    private static List<SKPoint> GeneratePoints(Random rng, int w, int h, int count)
    {
        var pts = new List<SKPoint>(count + 40);

        // Exact corners
        pts.Add(new SKPoint(0, 0));
        pts.Add(new SKPoint(w, 0));
        pts.Add(new SKPoint(0, h));
        pts.Add(new SKPoint(w, h));

        // Edge anchors to ensure border mesh coverage
        int edgesX = 12;
        int edgesY = 8;
        for (int i = 1; i < edgesX; i++)
        {
            float fx = (float)i / edgesX * w;
            pts.Add(new SKPoint(fx, 0));
            pts.Add(new SKPoint(fx, h));
        }
        for (int i = 1; i < edgesY; i++)
        {
            float fy = (float)i / edgesY * h;
            pts.Add(new SKPoint(0, fy));
            pts.Add(new SKPoint(w, fy));
        }

        // Random interior points with a slight margin away from border
        for (int i = 0; i < count; i++)
        {
            float x = 2f + rng.NextSingle() * (w - 4f);
            float y = 2f + rng.NextSingle() * (h - 4f);
            pts.Add(new SKPoint(x, y));
        }

        return pts;
    }

    // ── Bowyer–Watson Delaunay triangulation ──────────────────────────────────

    private readonly record struct Triangle(SKPoint A, SKPoint B, SKPoint C);

    private readonly record struct UndirectedEdge(SKPoint P, SKPoint Q)
    {
        public bool Equals(UndirectedEdge other) =>
            (P == other.P && Q == other.Q) || (P == other.Q && Q == other.P);

        public override int GetHashCode() =>
            HashCode.Combine(
                MathF.Min(P.X, Q.X), MathF.Min(P.Y, Q.Y),
                MathF.Max(P.X, Q.X), MathF.Max(P.Y, Q.Y));
    }

    private static List<Triangle> BowyerWatson(List<SKPoint> points, int width, int height)
    {
        // Generous super-triangle bounding the entire canvas domain
        float dmax = MathF.Max(width, height);
        float xmid = width * 0.5f;
        float ymid = height * 0.5f;

        var superA = new SKPoint(xmid - 20f * dmax, ymid - dmax);
        var superB = new SKPoint(xmid, ymid + 20f * dmax);
        var superC = new SKPoint(xmid + 20f * dmax, ymid - dmax);

        var triangles = new List<Triangle> { new(superA, superB, superC) };

        foreach (var pt in points)
        {
            var badTriangles = new List<Triangle>();
            foreach (var t in triangles)
            {
                if (InCircumcircle(t, pt))
                {
                    badTriangles.Add(t);
                }
            }

            // Boundary edges are those that belong to exactly ONE bad triangle.
            var edgeCount = new Dictionary<UndirectedEdge, int>();
            foreach (var bad in badTriangles)
            {
                AddEdge(edgeCount, bad.A, bad.B);
                AddEdge(edgeCount, bad.B, bad.C);
                AddEdge(edgeCount, bad.C, bad.A);
            }

            foreach (var bad in badTriangles)
            {
                triangles.Remove(bad);
            }

            foreach (var (edge, count) in edgeCount)
            {
                if (count == 1)
                {
                    triangles.Add(new Triangle(edge.P, edge.Q, pt));
                }
            }
        }

        // Remove any triangle sharing a vertex with the initial super-triangle
        triangles.RemoveAll(t =>
            IsSuperVertex(t.A, superA, superB, superC) ||
            IsSuperVertex(t.B, superA, superB, superC) ||
            IsSuperVertex(t.C, superA, superB, superC));

        return triangles;
    }

    private static void AddEdge(Dictionary<UndirectedEdge, int> counts, SKPoint p, SKPoint q)
    {
        var edge = new UndirectedEdge(p, q);
        counts[edge] = counts.GetValueOrDefault(edge) + 1;
    }

    private static bool IsSuperVertex(SKPoint p, SKPoint sA, SKPoint sB, SKPoint sC) =>
        p == sA || p == sB || p == sC;

    /// <summary>
    /// Geometric circumcircle test using circumcenter and radius squared.
    /// Completely invariant to vertex winding order (clockwise vs counter-clockwise).
    /// </summary>
    private static bool InCircumcircle(Triangle t, SKPoint p)
    {
        float ax = t.A.X, ay = t.A.Y;
        float bx = t.B.X, by = t.B.Y;
        float cx = t.C.X, cy = t.C.Y;

        float d = 2f * (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by));
        if (MathF.Abs(d) < 1e-6f) return false;

        float aSq = ax * ax + ay * ay;
        float bSq = bx * bx + by * by;
        float cSq = cx * cx + cy * cy;

        float ox = (aSq * (by - cy) + bSq * (cy - ay) + cSq * (ay - by)) / d;
        float oy = (aSq * (cx - bx) + bSq * (ax - cx) + cSq * (bx - ax)) / d;

        float rSq = (ax - ox) * (ax - ox) + (ay - oy) * (ay - oy);
        float distSq = (p.X - ox) * (p.X - ox) + (p.Y - oy) * (p.Y - oy);

        return distSq <= rSq + 1e-5f;
    }

    private static void SaveJpeg(SKSurface surface, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var image = surface.Snapshot();
        using var data  = image.Encode(SKEncodedImageFormat.Jpeg, 100);
        File.WriteAllBytes(path, data.ToArray());
    }
}
