using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using TodayWallpaper.Core.Weather;
using Xunit;

namespace TodayWallpaper.Tests.Weather;

public class WeatherServiceTests
{
    private readonly Mock<ILogger<WeatherService>> _loggerMock = new();

    private IHttpClientFactory CreateMockFactory(HttpResponseMessage message)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(message);

        var httpClient = new HttpClient(handlerMock.Object);
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient(nameof(WeatherService))).Returns(httpClient);
        return factoryMock.Object;
    }

    [Fact]
    public async Task GetCurrentWeatherAsync_ValidResponse_ParsesCorrectly()
    {
        var json = """
        {
            "latitude": 40.4,
            "longitude": -3.7,
            "current": {
                "time": "2026-09-05T08:00",
                "interval": 900,
                "temperature_2m": 22.5,
                "apparent_temperature": 21.8,
                "weathercode": 0,
                "windspeed_10m": 12.4,
                "relativehumidity_2m": 45,
                "precipitation": 0.0,
                "cloudcover": 10,
                "visibility": 10000.0,
                "dewpoint_2m": 9.5
            }
        }
        """;

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var factory = CreateMockFactory(response);
        var service = new WeatherService(factory, _loggerMock.Object);

        var weather = await service.GetCurrentWeatherAsync(40.4, -3.7, "Madrid");

        Assert.Equal("Madrid", weather.LocationName);
        Assert.Equal(WeatherCondition.Sunny, weather.Condition);
        Assert.Equal(0, weather.WmoCode);
        Assert.Equal(22.5, weather.TemperatureCelsius);
        Assert.Equal(21.8, weather.ApparentTemperatureCelsius);
        Assert.Equal(12.4, weather.WindspeedKmh);
        Assert.Equal(45, weather.Humidity);
        Assert.Equal(0.0, weather.PrecipitationMm);
        Assert.Equal(10, weather.CloudCoverPercent);
        Assert.Equal(10.0, weather.VisibilityKm);
        Assert.Equal(9.5, weather.DewpointCelsius);
    }

    [Fact]
    public async Task GetCurrentWeatherAsync_MissingCurrentBlock_ThrowsException()
    {
        var json = """
        {
            "latitude": 40.4,
            "longitude": -3.7
        }
        """;

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var factory = CreateMockFactory(response);
        var service = new WeatherService(factory, _loggerMock.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetCurrentWeatherAsync(40.4, -3.7, "Madrid"));
    }

    [Fact]
    public async Task GetCurrentWeatherAsync_UnderCommaCulture_FormatsUrlWithDots()
    {
        var originalCulture = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("es-ES");

            HttpRequestMessage? capturedRequest = null;
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("""
                    {
                        "latitude": 40.4168,
                        "longitude": -3.7038,
                        "current": {
                            "time": "2026-09-07T05:00",
                            "weathercode": 0
                        }
                    }
                    """, Encoding.UTF8, "application/json")
                });

            var httpClient = new HttpClient(handlerMock.Object);
            var factoryMock = new Mock<IHttpClientFactory>();
            factoryMock.Setup(f => f.CreateClient(nameof(WeatherService))).Returns(httpClient);

            var service = new WeatherService(factoryMock.Object, _loggerMock.Object);
            await service.GetCurrentWeatherAsync(40.4168, -3.7038, "Madrid");

            Assert.NotNull(capturedRequest);
            var query = capturedRequest.RequestUri?.Query;
            Assert.Contains("latitude=40.4168", query);
            Assert.Contains("longitude=-3.7038", query);
            Assert.DoesNotContain("latitude=40,4168", query);
            Assert.DoesNotContain("longitude=-3,7038", query);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
