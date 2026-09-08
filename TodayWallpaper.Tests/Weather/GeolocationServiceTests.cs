using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using TodayWallpaper.Core.Settings;
using TodayWallpaper.Core.Weather;
using Xunit;

namespace TodayWallpaper.Tests.Weather;

public class GeolocationServiceTests
{
    private readonly Mock<ISettingsStore> _settingsStoreMock = new();
    private readonly Mock<ILogger<GeolocationService>> _loggerMock = new();

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
        factoryMock.Setup(f => f.CreateClient(nameof(GeolocationService))).Returns(httpClient);
        return factoryMock.Object;
    }

    [Fact]
    public async Task GetLocationAsync_UsesManualLocation_WhenConfigured()
    {
        var settings = new AppSettings
        {
            Location = new LocationSettings(41.3851, 2.1734, "Barcelona")
        };

        _settingsStoreMock.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var factoryMock = new Mock<IHttpClientFactory>();
        var service = new GeolocationService(factoryMock.Object, _settingsStoreMock.Object, _loggerMock.Object);

        var loc = await service.GetLocationAsync();

        Assert.Equal("Barcelona", loc.City);
        Assert.Equal(41.3851, loc.Latitude);
        Assert.Equal(2.1734, loc.Longitude);
        factoryMock.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetLocationAsync_UsesCachedLocation_WhenAvailable()
    {
        var cached = new LocationInfo(51.5074, -0.1278, "London", "United Kingdom");
        var settings = new AppSettings
        {
            Location = null,
            CachedLocation = cached
        };

        _settingsStoreMock.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var factoryMock = new Mock<IHttpClientFactory>();
        var service = new GeolocationService(factoryMock.Object, _settingsStoreMock.Object, _loggerMock.Object);

        var loc = await service.GetLocationAsync();

        Assert.Equal("London", loc.City);
        Assert.Equal("United Kingdom", loc.Country);
        Assert.Equal(51.5074, loc.Latitude);
        Assert.Equal(-0.1278, loc.Longitude);
        factoryMock.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetLocationAsync_FetchesFromApiAndCaches_WhenNoManualOrCached()
    {
        var settings = new AppSettings
        {
            Location = null,
            CachedLocation = null
        };

        _settingsStoreMock.Setup(s => s.LoadAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(settings);

        var json = """
        {
            "lat": 48.8566,
            "lon": 2.3522,
            "city": "Paris",
            "country": "France"
        }
        """;

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        var factory = CreateMockFactory(response);
        var service = new GeolocationService(factory, _settingsStoreMock.Object, _loggerMock.Object);

        var loc = await service.GetLocationAsync();

        Assert.Equal("Paris", loc.City);
        Assert.Equal("France", loc.Country);
        Assert.Equal(48.8566, loc.Latitude);
        Assert.Equal(2.3522, loc.Longitude);

        _settingsStoreMock.Verify(s => s.SaveAsync(
            It.Is<AppSettings>(st => st.CachedLocation != null && st.CachedLocation.City == "Paris"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
