using System.Windows;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TodayWallpaper.App.ViewModels;
using TodayWallpaper.App.Views;
using TodayWallpaper.Core.Localization;
using TodayWallpaper.Core.Settings;
using TodayWallpaper.Core.Wallpaper;
using Application = System.Windows.Application;

namespace TodayWallpaper.App;

/// <summary>Manages the system tray icon and its context menu.</summary>
public sealed class TrayIconService(
    IServiceProvider services,
    ISettingsStore settingsStore,
    IWallpaperSetter wallpaperSetter,
    ILogger<TrayIconService> logger) : IDisposable
{
    private NotifyIcon? _notifyIcon;
    private MainWindow? _mainWindow;
    private bool _disposed;

    /// <summary>Creates and displays the tray icon.</summary>
    public void Initialize()
    {
        _notifyIcon = new NotifyIcon
        {
            Text    = Strings.TRAY_TOOLTIP,
            Visible = true,
            Icon    = System.Drawing.SystemIcons.Application
        };

        _notifyIcon.ContextMenuStrip = BuildMenu();
        _notifyIcon.DoubleClick     += (_, _) => ShowMainWindow();
    }

    /// <summary>Opens or restores the primary management window.</summary>
    public void ShowMainWindow()
    {
        if (_mainWindow is null || !_mainWindow.IsLoaded)
        {
            _mainWindow = services.GetRequiredService<MainWindow>();
            _mainWindow.DataContext = services.GetRequiredService<MainViewModel>();
            _mainWindow.Closed += (_, _) => _mainWindow = null;
            _mainWindow.Show();
        }
        else
        {
            if (_mainWindow.WindowState == WindowState.Minimized)
                _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();
            _mainWindow.Focus();
        }
    }

    // ── context menu ─────────────────────────────────────────────────────────

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();

        var title = new ToolStripLabel(Strings.TRAY_MENU_HEADER)
        {
            Font = new System.Drawing.Font("Segoe UI", 9, System.Drawing.FontStyle.Bold)
        };
        menu.Items.Add(title);
        menu.Items.Add(new ToolStripSeparator());

        menu.Items.Add(Strings.TRAY_MENU_OPEN_APP, null, (_, _) => ShowMainWindow());
        menu.Items.Add(Strings.TRAY_MENU_UPDATE_NOW, null, async (_, _) => await TriggerManualUpdateAsync());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(Strings.TRAY_MENU_PAUSE_RESUME, null, async (_, _) => await TogglePauseAsync());
        menu.Items.Add(Strings.TRAY_MENU_RESTORE_BG, null, (_, _) => RestoreWallpaper());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(Strings.TRAY_MENU_EXIT, null, (_, _) => ExitApp());

        return menu;
    }

    // ── actions ──────────────────────────────────────────────────────────────

    private async Task TriggerManualUpdateAsync()
    {
        try
        {
            _notifyIcon?.ShowBalloonTip(1500, Strings.TRAY_BALLOON_TITLE, Strings.TRAY_BALLOON_GENERATING, ToolTipIcon.Info);
            var pipeline = services.GetRequiredService<WallpaperPipeline>();
            var success = await pipeline.RunAsync(force: true);

            if (success)
            {
                _notifyIcon?.ShowBalloonTip(2000, Strings.TRAY_BALLOON_TITLE, Strings.TRAY_BALLOON_UPDATE_SUCCESS, ToolTipIcon.Info);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed manual update from tray.");
            _notifyIcon?.ShowBalloonTip(3000, Strings.TRAY_BALLOON_TITLE, Strings.Format(Strings.TRAY_BALLOON_UPDATE_ERROR, ex.Message), ToolTipIcon.Error);
        }
    }

    private async Task TogglePauseAsync()
    {
        var settings = await settingsStore.LoadAsync();
        var updated  = settings with { IsEnabled = !settings.IsEnabled };
        await settingsStore.SaveAsync(updated);

        string state = updated.IsEnabled ? Strings.TRAY_STATE_RESUMED : Strings.TRAY_STATE_PAUSED;
        logger.LogInformation("TodayWallpaper {State} by user.", state);
        _notifyIcon!.ShowBalloonTip(2000, Strings.TRAY_BALLOON_TITLE, Strings.Format(Strings.TRAY_BALLOON_STATE_CHANGE, state), ToolTipIcon.Info);
    }

    private void RestoreWallpaper()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title  = Strings.TRAY_DIALOG_RESTORE_TITLE,
            Filter = Strings.TRAY_DIALOG_RESTORE_FILTER
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            wallpaperSetter.Set(dialog.FileName);

            // Pause auto-refresh when user manually sets a wallpaper.
            _ = Task.Run(async () =>
            {
                var s = await settingsStore.LoadAsync();
                await settingsStore.SaveAsync(s with { IsEnabled = false });
            });

            logger.LogInformation("User restored wallpaper: {Path}", dialog.FileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to restore wallpaper.");
            System.Windows.MessageBox.Show(Strings.Format(Strings.TRAY_MSGBOX_ERROR_MSG, ex.Message), Strings.TRAY_MSGBOX_ERROR_TITLE,
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static void ExitApp()
    {
        Application.Current.Shutdown();
    }

    // ── IDisposable ──────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _notifyIcon?.Dispose();
        _disposed = true;
    }
}
