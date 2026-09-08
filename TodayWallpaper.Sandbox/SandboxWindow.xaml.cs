using System.Windows;

namespace TodayWallpaper.Sandbox;

/// <summary>
/// Interaction logic for SandboxWindow.xaml
/// </summary>
public partial class SandboxWindow : Window
{
    private SandboxViewModel ViewModel => (SandboxViewModel)DataContext;

    public SandboxWindow(SandboxViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void RandomizeClick(object sender, RoutedEventArgs e)
    {
        ViewModel.Randomize();
    }

    private async void GenerateClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.GenerateAsync();
    }

    private async void SaveAsClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.SaveAsAsync();
    }

    private async void ApplyClick(object sender, RoutedEventArgs e)
    {
        await ViewModel.ApplyToDesktopAsync();
    }
}
