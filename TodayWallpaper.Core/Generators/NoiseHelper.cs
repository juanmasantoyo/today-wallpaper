namespace TodayWallpaper.Core.Generators;

/// <summary>
/// Provides a fast, self-contained 2D value-noise implementation.
/// Used by procedural generators that need smooth field-like noise without external dependencies.
/// </summary>
internal static class NoiseHelper
{
    // ── 2-D value noise with smoothstep interpolation ────────────────────────

    /// <summary>
    /// Returns a pseudo-random float in [0, 1] for integer grid coordinates.
    /// Uses a classic hash to break lattice regularity.
    /// </summary>
    private static float Hash(int x, int y)
    {
        int n = x + y * 57;
        n = (n << 13) ^ n;
        return (1f - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7FFFFFFF) / 1073741824f) * 0.5f + 0.5f;
    }

    /// <summary>Cubic smoothstep: 3t² − 2t³.</summary>
    private static float Smooth(float t) => t * t * (3f - 2f * t);

    /// <summary>
    /// Returns smooth 2-D value noise in [0, 1].
    /// </summary>
    public static float Noise(float x, float y)
    {
        int ix = (int)MathF.Floor(x);
        int iy = (int)MathF.Floor(y);
        float fx = x - ix;
        float fy = y - iy;

        float ux = Smooth(fx);
        float uy = Smooth(fy);

        float a = Hash(ix,     iy);
        float b = Hash(ix + 1, iy);
        float c = Hash(ix,     iy + 1);
        float d = Hash(ix + 1, iy + 1);

        return a + (b - a) * ux
                 + (c - a) * uy
                 + (a - b - c + d) * ux * uy;
    }

    /// <summary>
    /// Fractal (fBm) noise — sums <paramref name="octaves"/> layers with increasing frequency.
    /// Returns a value in [0, 1].
    /// </summary>
    public static float FractalNoise(float x, float y, int octaves = 6, float lacunarity = 2f, float gain = 0.5f)
    {
        float value    = 0f;
        float amplitude = 0.5f;
        float frequency = 1f;
        float max      = 0f;

        for (int i = 0; i < octaves; i++)
        {
            value    += Noise(x * frequency, y * frequency) * amplitude;
            max      += amplitude;
            amplitude *= gain;
            frequency *= lacunarity;
        }

        return value / max;
    }
}
