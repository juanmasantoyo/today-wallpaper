using System.Globalization;
using TodayWallpaper.Core.Localization;
using Xunit;

namespace TodayWallpaper.Tests.Localization;

public class LocalizationTests : IDisposable
{
    public LocalizationTests()
    {
        Strings.Culture = null;
    }

    public void Dispose()
    {
        Strings.Culture = null;
    }

    [Fact]
    public void SpanishCulture_ReturnsSpanishTranslations()
    {
        Strings.Culture = new CultureInfo("es-ES");

        Assert.Equal("Today Wallpaper", Strings.APP_MAIN_WINDOW_TITLE);
        Assert.Equal("Todos los días a las:", Strings.APP_RADIO_DAILY);
        Assert.Equal("Establecer como fondo", Strings.APP_BTN_HISTORY_APPLY);
        Assert.Equal("Programación desactivada (Pausada)", Strings.VM_SCHEDULE_SUMMARY_PAUSED);
        Assert.Equal("Listo.", Strings.VM_STATUS_DEFAULT);
        Assert.Equal("Actualizar fondo ahora", Strings.TRAY_MENU_UPDATE_NOW);
        Assert.Equal("Parámetros Meteorológicos", Strings.SANDBOX_GROUP_WEATHER);
    }

    [Fact]
    public void EnglishCulture_ReturnsEnglishTranslations()
    {
        Strings.Culture = new CultureInfo("en-US");

        Assert.Equal("Today Wallpaper", Strings.APP_MAIN_WINDOW_TITLE);
        Assert.Equal("Every day at:", Strings.APP_RADIO_DAILY);
        Assert.Equal("Set as wallpaper", Strings.APP_BTN_HISTORY_APPLY);
        Assert.Equal("Scheduling disabled (Paused)", Strings.VM_SCHEDULE_SUMMARY_PAUSED);
        Assert.Equal("Ready.", Strings.VM_STATUS_DEFAULT);
        Assert.Equal("Update wallpaper now", Strings.TRAY_MENU_UPDATE_NOW);
        Assert.Equal("Weather Parameters", Strings.SANDBOX_GROUP_WEATHER);
    }

    [Fact]
    public void OtherCulture_FallsBackToEnglish()
    {
        Strings.Culture = new CultureInfo("fr-FR");

        Assert.Equal("Every day at:", Strings.APP_RADIO_DAILY);
        Assert.Equal("Ready.", Strings.VM_STATUS_DEFAULT);
        Assert.Equal("Update wallpaper now", Strings.TRAY_MENU_UPDATE_NOW);
    }

    [Fact]
    public void Format_SpanishAndEnglish_FormatsCorrectly()
    {
        Strings.Culture = new CultureInfo("es-ES");
        var esFormat = Strings.Format(Strings.VM_STATUS_DETECT_IP_SUCCESS, "Madrid", "España");
        Assert.Equal("Ubicación detectada: Madrid, España.", esFormat);

        Strings.Culture = new CultureInfo("en-US");
        var enFormat = Strings.Format(Strings.VM_STATUS_DETECT_IP_SUCCESS, "Madrid", "Spain");
        Assert.Equal("Location detected: Madrid, Spain.", enFormat);
    }
}
