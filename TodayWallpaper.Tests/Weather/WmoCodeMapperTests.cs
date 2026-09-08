using TodayWallpaper.Core.Weather;
using Xunit;

namespace TodayWallpaper.Tests.Weather;

public class WmoCodeMapperTests
{
    [Theory]
    [InlineData(0, WeatherCondition.Sunny)]
    [InlineData(1, WeatherCondition.Cloudy)]
    [InlineData(2, WeatherCondition.Cloudy)]
    [InlineData(3, WeatherCondition.Cloudy)]
    [InlineData(45, WeatherCondition.Foggy)]
    [InlineData(48, WeatherCondition.Foggy)]
    [InlineData(51, WeatherCondition.Rainy)]
    [InlineData(61, WeatherCondition.Rainy)]
    [InlineData(67, WeatherCondition.Rainy)]
    [InlineData(71, WeatherCondition.Snowy)]
    [InlineData(75, WeatherCondition.Snowy)]
    [InlineData(77, WeatherCondition.Snowy)]
    [InlineData(80, WeatherCondition.Rainy)]
    [InlineData(81, WeatherCondition.Rainy)]
    [InlineData(82, WeatherCondition.Rainy)]
    [InlineData(85, WeatherCondition.Snowy)]
    [InlineData(86, WeatherCondition.Snowy)]
    [InlineData(95, WeatherCondition.Stormy)]
    [InlineData(96, WeatherCondition.Stormy)]
    [InlineData(99, WeatherCondition.Stormy)]
    [InlineData(-1, WeatherCondition.Cloudy)]
    [InlineData(999, WeatherCondition.Cloudy)]
    public void Map_MapsCorrectly(int code, WeatherCondition expected)
    {
        var result = WmoCodeMapper.Map(code);
        Assert.Equal(expected, result);
    }
}
