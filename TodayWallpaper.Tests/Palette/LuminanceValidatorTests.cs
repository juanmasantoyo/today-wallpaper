using SkiaSharp;
using TodayWallpaper.Core.Palette;
using Xunit;

namespace TodayWallpaper.Tests.Palette;

public class LuminanceValidatorTests
{
    [Fact]
    public void IsAcceptable_TooDarkColors_ReturnsFalse()
    {
        var black = new SKColor(0, 0, 0);
        var pitchDarkNavy = new SKColor(10, 15, 35);
        var stormyNearBlack1 = SKColor.Parse("#0D0D1F");
        var stormyNearBlack2 = SKColor.Parse("#001A1A");

        Assert.False(LuminanceValidator.IsAcceptable(black));
        Assert.False(LuminanceValidator.IsAcceptable(pitchDarkNavy));
        Assert.False(LuminanceValidator.IsAcceptable(stormyNearBlack1));
        Assert.False(LuminanceValidator.IsAcceptable(stormyNearBlack2));
    }

    [Fact]
    public void IsAcceptable_ValidMidTones_ReturnsTrue()
    {
        var midGray = new SKColor(100, 100, 100);
        var deepIndigo = SKColor.FromHsl(230, 50, 25f);
        var stormyBlue = SKColor.FromHsl(220, 40, 30f);

        Assert.True(LuminanceValidator.IsAcceptable(midGray));
        Assert.True(LuminanceValidator.IsAcceptable(deepIndigo));
        Assert.True(LuminanceValidator.IsAcceptable(stormyBlue));
    }

    [Fact]
    public void IsAcceptable_VeryBrightColors_ReturnsFalse()
    {
        var pureWhite = new SKColor(255, 255, 255);
        var brightYellow = new SKColor(255, 255, 100);
        var lightCyan = new SKColor(200, 240, 255);

        Assert.False(LuminanceValidator.IsAcceptable(pureWhite));
        Assert.False(LuminanceValidator.IsAcceptable(brightYellow));
        Assert.False(LuminanceValidator.IsAcceptable(lightCyan));
    }

    [Fact]
    public void IsAcceptable_BoundaryCheck()
    {
        // Lower boundary (20%)
        var belowMin = SKColor.FromHsl(220, 50, 18f);
        var atMin = SKColor.FromHsl(220, 50, 20f);

        Assert.False(LuminanceValidator.IsAcceptable(belowMin));
        Assert.True(LuminanceValidator.IsAcceptable(atMin));

        // Upper boundary (55%)
        var atMax = SKColor.FromHsl(0, 50, 55f);
        var aboveMax = SKColor.FromHsl(0, 50, 57f);

        Assert.True(LuminanceValidator.IsAcceptable(atMax));
        Assert.False(LuminanceValidator.IsAcceptable(aboveMax));
    }
}
