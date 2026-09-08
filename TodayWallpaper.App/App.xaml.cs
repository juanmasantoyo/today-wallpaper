using System.Globalization;
using System.Windows.Markup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Windows;
using Application = System.Windows.Application;
using TodayWallpaper.Core;
using TodayWallpaper.Core.Localization;
using TodayWallpaper.App.ViewModels;
using TodayWallpaper.App.Views;

namespace TodayWallpaper.App;

/// <summary>Application entry point. Initializes services, tray icon, and launches the main window.</summary>
public partial class App : Application
{
    private IHost? _host;
    private TrayIconService? _trayIcon;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Detect system culture: Spanish if the PC uses Spanish, English fallback for any other language.
        var isSpanish = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.Equals("es", StringComparison.OrdinalIgnoreCase);
        var culture = isSpanish ? new CultureInfo("es-ES") : new CultureInfo("en-US");

        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        Strings.Culture = culture;

        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddTodayWallpaperCore();
                services.AddTransient<MainViewModel>();
                services.AddTransient<MainWindow>();
                services.AddTransient<HistoryViewModel>();
                services.AddTransient<SettingsViewModel>();
                services.AddSingleton<TrayIconService>();
            })
            .Build();

        await _host.StartAsync();

        _trayIcon = _host.Services.GetRequiredService<TrayIconService>();
        _trayIcon.Initialize();

        // Show the main management window on application startup
        _trayIcon.ShowMainWindow();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_trayIcon is not null)
            _trayIcon.Dispose();

        if (_host is not null)
        {
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }

        base.OnExit(e);
    }
}
