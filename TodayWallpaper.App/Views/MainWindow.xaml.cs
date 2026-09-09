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

    // ── Color Action handlers ────────────────────────────────────────────────

    private void RemovePrimaryColorClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: PaletteColorViewModel item })
            ViewModel?.RemovePrimaryColor(item);
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

                element.Opacity = 0.35;
                try
                {
                    var data = new System.Windows.DataObject(typeof(PaletteColorViewModel), colorToDrag);
                    System.Windows.DragDrop.DoDragDrop(element, data, System.Windows.DragDropEffects.Move);
                }
                finally
                {
                    element.Opacity = 1.0;
                    ResetDropZoneVisuals();
                }
            }
        }
    }

    private void ColorDropZone_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(PaletteColorViewModel)) && sender is System.Windows.Controls.Border border)
        {
            border.Background = TryFindResource("CardSelectedBackgroundBrush") as System.Windows.Media.Brush ?? System.Windows.Media.Brushes.AliceBlue;
            border.BorderBrush = TryFindResource("AccentBrush") as System.Windows.Media.Brush ?? System.Windows.Media.Brushes.DodgerBlue;
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

    private void ColorDropZone_DragLeave(object sender, DragEventArgs e)
    {
        if (sender is System.Windows.Controls.Border border)
        {
            border.Background = System.Windows.Media.Brushes.Transparent;
            border.BorderBrush = System.Windows.Media.Brushes.Transparent;
        }
    }

    private void ColorDropZone_Drop(object sender, DragEventArgs e)
    {
        ResetDropZoneVisuals();

        if (ViewModel?.SelectedPalette is null) return;

        if (e.Data.GetData(typeof(PaletteColorViewModel)) is PaletteColorViewModel draggedItem)
        {
            bool isDropOnSecondary = sender == SecondaryDropZone;
            ViewModel.MoveColor(draggedItem, toSecondary: isDropOnSecondary);
            e.Handled = true;
        }
    }

    private void ResetDropZoneVisuals()
    {
        if (PrimaryDropZone is not null)
        {
            PrimaryDropZone.Background = System.Windows.Media.Brushes.Transparent;
            PrimaryDropZone.BorderBrush = System.Windows.Media.Brushes.Transparent;
        }
        if (SecondaryDropZone is not null)
        {
            SecondaryDropZone.Background = System.Windows.Media.Brushes.Transparent;
            SecondaryDropZone.BorderBrush = System.Windows.Media.Brushes.Transparent;
        }
    }
}
