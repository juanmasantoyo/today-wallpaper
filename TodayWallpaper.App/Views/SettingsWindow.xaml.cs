using System.Windows;
using TodayWallpaper.App.ViewModels;

namespace TodayWallpaper.App.Views;

/// <summary>Settings window code-behind.</summary>
public partial class SettingsWindow : Window
{
    private SettingsViewModel? ViewModel => DataContext as SettingsViewModel;

    /// <summary>Initialises SettingsWindow and loads current settings.</summary>
    public SettingsWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await (ViewModel?.LoadAsync() ?? Task.CompletedTask);
    }

    private async void SaveClick(object sender, RoutedEventArgs e)
    {
        await (ViewModel?.SaveAsync() ?? Task.CompletedTask);
        DialogResult = true;
        Close();
    }

    private void CancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
