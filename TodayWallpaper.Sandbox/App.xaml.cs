using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TodayWallpaper.Core;
using TodayWallpaper.Core.Localization;

namespace TodayWallpaper.Sandbox;

/// <summary>
/// Application entry point for TodayWallpaper Sandbox.
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

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

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTodayWallpaperCore();
        services.AddTransient<SandboxViewModel>();
        services.AddTransient<SandboxWindow>();

        _serviceProvider = services.BuildServiceProvider();

        var window = _serviceProvider.GetRequiredService<SandboxWindow>();
        window.Show();

        var vm = (SandboxViewModel)window.DataContext;
        vm.Initialize();
        await vm.GenerateAsync();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
