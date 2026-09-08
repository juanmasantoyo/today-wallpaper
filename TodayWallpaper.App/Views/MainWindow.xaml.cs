using System.Windows;
using System.Windows.Input;
using TodayWallpaper.App.ViewModels;
using DragEventArgs = System.Windows.DragEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;

namespace TodayWallpaper.App.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml - TodayWallpaper Primary Management Window.
/// </summary>
public partial class MainWindow : Window
{
    private MainViewModel? ViewModel => DataContext as MainViewModel;
    private Point _dragStartPoint;
    private PaletteColorViewModel? _draggedColor;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (ViewModel is not null)
            {
                await ViewModel.InitializeAsync();
            }
        };
        Closing += async (_, _) =>
        {
            if (ViewModel is not null)
            {
                await ViewModel.FlushPendingSavesAsync();
            }
        };
    }

    private async void GenerateNowClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
            await ViewModel.GenerateNowAsync();
    }

    private async void DetectLocationClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
            await ViewModel.DetectLocationAsync();
    }

    private async void RefreshWeatherClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
            await ViewModel.RefreshWeatherAsync();
    }

    private void ApplyClick(object sender, RoutedEventArgs e)
    {
        ViewModel?.ApplySelectedWallpaper();
    }

    private async void PinClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
            await ViewModel.TogglePinSelectedAsync();
    }

    private async void DeleteClick(object sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
            await ViewModel.DeleteSelectedAsync();
    }

    private void OpenViewerClick(object sender, RoutedEventArgs e)
    {
        ViewModel?.OpenSelectedInViewer();
    }

    // ── Palette CRUD handlers ────────────────────────────────────────────────

    private void NewPaletteClick(object sender, RoutedEventArgs e)
    {
        ViewModel?.AddNewPalette();
    }

    private void DuplicatePaletteClick(object sender, RoutedEventArgs e)
    {
        ViewModel?.DuplicateCurrentPalette();
    }

    private void DeletePaletteClick(object sender, RoutedEventArgs e)
    {
        ViewModel?.DeleteCurrentPalette();
    }

    // ── Color Action handlers ────────────────────────────────────────────────

    private void AddPrimaryColorClick(object sender, RoutedEventArgs e)
    {
        ViewModel?.AddPrimaryColor();
    }

    private void RemovePrimaryColorClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: PaletteColorViewModel item })
            ViewModel?.RemovePrimaryColor(item);
    }

    private void AddSecondaryColorClick(object sender, RoutedEventArgs e)
    {
        ViewModel?.AddSecondaryColor();
    }

    private void RemoveSecondaryColorClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: PaletteColorViewModel item })
            ViewModel?.RemoveSecondaryColor(item);
    }

    private void PickColorClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: PaletteColorViewModel item })
            ViewModel?.PickColor(item);
    }

    private void SwatchPreview_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount >= 1 && sender is FrameworkElement { Tag: PaletteColorViewModel item })
        {
            ViewModel?.PickColor(item);
        }
    }

    private void ResetConditionClick(object sender, RoutedEventArgs e)
    {
        ViewModel?.ResetCurrentConditionPalettes();
    }

    private void ResetAllPalettesClick(object sender, RoutedEventArgs e)
    {
        ViewModel?.ResetAllPalettes();
    }

    // ── Drag & Drop between Primary and Secondary Columns ────────────────────

    private void ColorItem_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && sender is FrameworkElement element && element.Tag is PaletteColorViewModel colorVm)
        {
            // Only start drag if not clicking directly inside a TextBox or Button
            if (e.OriginalSource is not System.Windows.Controls.TextBox &&
                e.OriginalSource is not System.Windows.Controls.Button)
            {
                _dragStartPoint = e.GetPosition(this);
                _draggedColor = colorVm;
            }
        }
    }

    private void ColorItem_MouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && _draggedColor is not null && sender is FrameworkElement element)
        {
            var currentPos = e.GetPosition(this);
            var diff = _dragStartPoint - currentPos;

            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                var colorToDrag = _draggedColor;
                _draggedColor = null;

                var data = new System.Windows.DataObject(typeof(PaletteColorViewModel), colorToDrag);
                System.Windows.DragDrop.DoDragDrop(element, data, System.Windows.DragDropEffects.Move);
            }
        }
    }

    private void ColorDropZone_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(PaletteColorViewModel)))
        {
            e.Effects = System.Windows.DragDropEffects.Move;
            e.Handled = true;
        }
        else
        {
            e.Effects = System.Windows.DragDropEffects.None;
        }
    }

    private void ColorDropZone_Drop(object sender, DragEventArgs e)
    {
        if (ViewModel?.SelectedPalette is null) return;

        if (e.Data.GetData(typeof(PaletteColorViewModel)) is PaletteColorViewModel draggedItem)
        {
            bool isDropOnSecondary = sender == SecondaryDropZone;
            ViewModel.MoveColor(draggedItem, toSecondary: isDropOnSecondary);
            e.Handled = true;
        }
    }
}
