using TodayWallpaper.Core.Generators;
using Xunit;

namespace TodayWallpaper.Tests.Generators;

public class NoiseHelperTests
{
    [Fact]
    public void Noise_ReturnsValuesWithinZeroAndOne()
    {
        for (float x = -10f; x <= 10f; x += 0.5f)
        {
            for (float y = -10f; y <= 10f; y += 0.5f)
            {
                float n = NoiseHelper.Noise(x, y);
                Assert.InRange(n, 0f, 1f);
            }
        }
    }

    [Fact]
    public void FractalNoise_ReturnsValuesWithinZeroAndOne()
    {
        for (float x = 0f; x <= 5f; x += 0.25f)
        {
            for (float y = 0f; y <= 5f; y += 0.25f)
            {
                float fn = NoiseHelper.FractalNoise(x, y);
                Assert.InRange(fn, 0f, 1f);
            }
        }
    }

    [Fact]
    public void Noise_IsDeterministic()
    {
        float n1 = NoiseHelper.Noise(12.34f, 56.78f);
        float n2 = NoiseHelper.Noise(12.34f, 56.78f);
        Assert.Equal(n1, n2);

        float fn1 = NoiseHelper.FractalNoise(42.1f, 84.2f);
        float fn2 = NoiseHelper.FractalNoise(42.1f, 84.2f);
        Assert.Equal(fn1, fn2);
    }
}
