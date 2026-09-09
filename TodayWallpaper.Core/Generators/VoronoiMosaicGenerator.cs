using Microsoft.Extensions.Logging;
using SkiaSharp;
using TodayWallpaper.Core.Palette;
using TodayWallpaper.Core.Weather;

namespace TodayWallpaper.Core.Generators;

/// <summary>
/// Generates a crystalline Voronoi mosaic wallpaper with illuminated cell facets and translucent edges.
/// Temperature determines the virtual light source position and warm/cool highlights;
/// humidity modulates edge softness and atmospheric glow.
/// </summary>
public sealed class VoronoiMosaicGenerator(ILogger<VoronoiMosaicGenerator> logger) : IWallpaperGenerator
{
    /// <inheritdoc/>
    public string StyleId => "VoronoiMosaic";

    /// <inheritdoc/>
    public Task<string> GenerateAsync(
        ColorPalette palette,
        WeatherData weather,
        int width,
        int height,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        logger.LogDebug("VoronoiMosaic: generating {W}×{H}", width, height);

        var rng = new Random(WeatherSeed.Build(weather));

        // Weather parameter mappings
        int cellCountX = (int)Math.Clamp(8 + (weather.WindspeedKmh / 15.0), 8, 14);
        int cellCountY = (int)Math.Clamp(5 + (weather.WindspeedKmh / 20.0), 5, 9);
        float edgeSoftness = (float)Math.Clamp(weather.Humidity / 100.0, 0.2, 0.9);

        // Virtual light position from temperature (warm/cool slant)
        float lightNormalizedX = (float)Math.Clamp((weather.TemperatureCelsius + 10.0) / 45.0, 0.2, 0.8);
        var lightPos = new SKPoint(width * lightNormalizedX, height * 0.2f);

        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;

        // 1. Base gradient background
        using (var bgShader = SKShader.CreateLinearGradient(
            new SKPoint(0, 0),
            new SKPoint(width, height),
            [palette.PrimaryColors[0], palette.PrimaryColors[^1]],
            SKShaderTileMode.Clamp))
        using (var bgPaint = new SKPaint { Shader = bgShader, IsAntialias = true, IsDither = true })
        {
            canvas.DrawRect(SKRect.Create(width, height), bgPaint);
        }

        // 2. Generate jittered grid sites (including border margins)
        var sites = GenerateJitteredSites(rng, width, height, cellCountX, cellCountY);

        // 3. Delaunay Triangulation (Bowyer-Watson)
        var triangles = BowyerWatson(sites, width, height);

        // 4. Build Voronoi cells (dual of Delaunay)
        var cells = BuildVoronoiCells(sites, triangles);

        var allColors = palette.PrimaryColors.Concat(palette.SecondaryColors).ToArray();

        // 5. Render Voronoi cells
        foreach (var cell in cells)
        {
            if (cell.Vertices.Count < 3) continue;

            using var cellPath = new SKPath();
            cellPath.MoveTo(cell.Vertices[0]);
            for (int i = 1; i < cell.Vertices.Count; i++)
            {
                cellPath.LineTo(cell.Vertices[i]);
            }
            cellPath.Close();

            // Pick color based on site position
            float nx = Math.Clamp(cell.Site.X / width, 0f, 1f);
            float ny = Math.Clamp(cell.Site.Y / height, 0f, 1f);
            int colorIdx = (int)((nx * 0.6f + ny * 0.4f) * (allColors.Length - 1)) % allColors.Length;
            var baseColor = allColors[colorIdx];

            // Direction towards virtual light source
            float dx = lightPos.X - cell.Site.X;
            float dy = lightPos.Y - cell.Site.Y;
            float distToLight = MathF.Sqrt(dx * dx + dy * dy);
            float maxDist = MathF.Sqrt(width * width + height * height);
            float lightIntensity = Math.Clamp(1.0f - (distToLight / maxDist) * 0.7f, 0.4f, 1.0f);

            var highlightColor = AdjustLightness(baseColor, lightIntensity * 1.15f);
            var shadowColor = AdjustLightness(baseColor, lightIntensity * 0.85f);

            // Radial gradient inside cell centered slightly toward light
            var focalPoint = new SKPoint(
                cell.Site.X + (dx / (distToLight + 1e-4f)) * 25f,
                cell.Site.Y + (dy / (distToLight + 1e-4f)) * 25f);

            float cellRadius = Math.Max(width / (float)cellCountX, height / (float)cellCountY) * 1.6f;

            using var cellShader = SKShader.CreateRadialGradient(
                focalPoint,
                cellRadius,
                [highlightColor, shadowColor],
                SKShaderTileMode.Clamp);

            using var cellFill = new SKPaint
            {
                Shader = cellShader,
                IsAntialias = true,
                IsDither = true
            };

            canvas.DrawPath(cellPath, cellFill);

            // Cell border stroke
            byte borderAlpha = (byte)Math.Clamp(50 + (1f - edgeSoftness) * 70, 35, 120);
            using var borderPaint = new SKPaint
            {
                Color = SKColors.White.WithAlpha(borderAlpha),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1.2f,
                IsAntialias = true,
                BlendMode = SKBlendMode.Overlay
            };
            canvas.DrawPath(cellPath, borderPaint);
        }

        // 6. Atmospheric diffusion vignette
        float darken = (float)Math.Clamp(weather.CloudCoverPercent / 2000.0, 0.0, 0.05);
        if (darken > 0.005f)
        {
            using var overlay = new SKPaint
            {
                IsAntialias = true,
                Color = new SKColor(0, 0, 0, (byte)(darken * 255))
            };
            canvas.DrawRect(SKRect.Create(width, height), overlay);
        }

        SaveImage(surface, outputPath);
        return Task.FromResult(outputPath);
    }

    private static List<SKPoint> GenerateJitteredSites(Random rng, int width, int height, int countX, int countY)
    {
        var points = new List<SKPoint>();
        float stepX = width / (float)countX;
        float stepY = height / (float)countY;

        // Margin in cells to prevent boundary artifacts
        for (int gy = -2; gy <= countY + 2; gy++)
        {
            for (int gx = -2; gx <= countX + 2; gx++)
            {
                float jx = (rng.NextSingle() - 0.5f) * 0.75f * stepX;
                float jy = (rng.NextSingle() - 0.5f) * 0.75f * stepY;
                points.Add(new SKPoint(gx * stepX + jx, gy * stepY + jy));
            }
        }

        return points;
    }

    private sealed record VoronoiCell(SKPoint Site, List<SKPoint> Vertices);

    private static List<VoronoiCell> BuildVoronoiCells(List<SKPoint> sites, List<Triangle> triangles)
    {
        var cells = new List<VoronoiCell>();

        // Map each site to triangles that share it
        var siteTriangles = new Dictionary<int, List<Triangle>>();
        for (int i = 0; i < sites.Count; i++)
        {
            siteTriangles[i] = [];
        }

        foreach (var tri in triangles)
        {
            for (int i = 0; i < sites.Count; i++)
            {
                var p = sites[i];
                if (p == tri.A || p == tri.B || p == tri.C)
                {
                    siteTriangles[i].Add(tri);
                }
            }
        }

        for (int i = 0; i < sites.Count; i++)
        {
            var site = sites[i];
            var triList = siteTriangles[i];
            if (triList.Count < 3) continue;

            // Voronoi vertices are circumcenters of adjacent triangles
            var circumcenters = triList.Select(t => t.Circumcenter).ToList();

            // Sort vertices angularly around the site
            circumcenters.Sort((v1, v2) =>
            {
                float a1 = MathF.Atan2(v1.Y - site.Y, v1.X - site.X);
                float a2 = MathF.Atan2(v2.Y - site.Y, v2.X - site.X);
                return a1.CompareTo(a2);
            });

            cells.Add(new VoronoiCell(site, circumcenters));
        }

        return cells;
    }

    private sealed record Triangle(SKPoint A, SKPoint B, SKPoint C, SKPoint Circumcenter, float RadiusSq);

    private static List<Triangle> BowyerWatson(List<SKPoint> pointList, int width, int height)
    {
        // Super-triangle enclosing all points
        float margin = Math.Max(width, height) * 6f;
        var stA = new SKPoint(width / 2f, -margin);
        var stB = new SKPoint(-margin, height + margin);
        var stC = new SKPoint(width + margin, height + margin);

        var triangles = new List<Triangle> { CreateTriangle(stA, stB, stC) };

        foreach (var pt in pointList)
        {
            var badTriangles = new List<Triangle>();
            foreach (var tri in triangles)
            {
                float dx = pt.X - tri.Circumcenter.X;
                float dy = pt.Y - tri.Circumcenter.Y;
                if (dx * dx + dy * dy <= tri.RadiusSq)
                    badTriangles.Add(tri);
            }

            // Find boundary of polygonal hole
            var polygon = new List<(SKPoint P1, SKPoint P2)>();
            foreach (var tri in badTriangles)
            {
                (SKPoint, SKPoint)[] edges = [(tri.A, tri.B), (tri.B, tri.C), (tri.C, tri.A)];
                foreach (var edge in edges)
                {
                    bool shared = badTriangles.Any(other =>
                        other != tri &&
                        ((other.A == edge.Item1 && other.B == edge.Item2) || (other.A == edge.Item2 && other.B == edge.Item1) ||
                         (other.B == edge.Item1 && other.C == edge.Item2) || (other.B == edge.Item2 && other.C == edge.Item1) ||
                         (other.C == edge.Item1 && other.A == edge.Item2) || (other.C == edge.Item2 && other.A == edge.Item1)));

                    if (!shared) polygon.Add(edge);
                }
            }

            foreach (var tri in badTriangles)
                triangles.Remove(tri);

            foreach (var (p1, p2) in polygon)
                triangles.Add(CreateTriangle(p1, p2, pt));
        }

        // Remove triangles that share vertices with super-triangle
        triangles.RemoveAll(t =>
            t.A == stA || t.A == stB || t.A == stC ||
            t.B == stA || t.B == stB || t.B == stC ||
            t.C == stA || t.C == stB || t.C == stC);

        return triangles;
    }

    private static Triangle CreateTriangle(SKPoint a, SKPoint b, SKPoint c)
    {
        float d = 2 * (a.X * (b.Y - c.Y) + b.X * (c.Y - a.Y) + c.X * (a.Y - b.Y));
        if (MathF.Abs(d) < 1e-6f)
        {
            return new Triangle(a, b, c, a, 0);
        }

        float aSq = a.X * a.X + a.Y * a.Y;
        float bSq = b.X * b.X + b.Y * b.Y;
        float cSq = c.X * c.X + c.Y * c.Y;

        float ux = (aSq * (b.Y - c.Y) + bSq * (c.Y - a.Y) + cSq * (a.Y - b.Y)) / d;
        float uy = (aSq * (c.X - b.X) + bSq * (a.X - c.X) + cSq * (b.X - a.X)) / d;
        var center = new SKPoint(ux, uy);

        float rSq = (a.X - ux) * (a.X - ux) + (a.Y - uy) * (a.Y - uy);
        return new Triangle(a, b, c, center, rSq);
    }

    private static SKColor AdjustLightness(SKColor color, float factor)
    {
        color.ToHsl(out float h, out float s, out float l);
        float newL = Math.Clamp(l * factor, 12f, 55f);
        return SKColor.FromHsl(h, s, newL, color.Alpha);
    }

    private static void SaveImage(SKSurface surface, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var image = surface.Snapshot();
        var format = path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
            ? SKEncodedImageFormat.Jpeg
            : SKEncodedImageFormat.Png;
        using var data = image.Encode(format, 100);
        File.WriteAllBytes(path, data.ToArray());
    }
}
