using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using SkiaSharp;
using TodayWallpaper.Core.Localization;
using TodayWallpaper.Core.Palette;

namespace TodayWallpaper.App.ViewModels;

/// <summary>
/// ViewModel representing a single color palette entry (primary and secondary colors, name, enabled state).
/// </summary>
public sealed class PaletteEntryViewModel : INotifyPropertyChanged
{
    private string _name;
    private bool _isEnabled;
    private ObservableCollection<PaletteColorViewModel> _primaryColors = [];
    private ObservableCollection<PaletteColorViewModel> _secondaryColors = [];
    private ObservableCollection<Brush> _previewBrushes = [];
    private bool _hasValidationErrors;
    private string _validationMessage = string.Empty;
    private readonly Action? _onChanged;

    public event PropertyChangedEventHandler? PropertyChanged;

    public PaletteEntryViewModel(
        string name,
        bool isEnabled,
        IEnumerable<SKColor> primary,
        IEnumerable<SKColor> secondary,
        Action? onChanged = null)
    {
        _name = string.IsNullOrWhiteSpace(name) ? "Default" : name.Trim();
        _isEnabled = isEnabled;
        _onChanged = onChanged;

        _primaryColors = new ObservableCollection<PaletteColorViewModel>(
            primary.Select(c => new PaletteColorViewModel($"#{c.Red:X2}{c.Green:X2}{c.Blue:X2}", OnColorChanged)));

        _secondaryColors = new ObservableCollection<PaletteColorViewModel>(
            secondary.Select(c => new PaletteColorViewModel($"#{c.Red:X2}{c.Green:X2}{c.Blue:X2}", OnColorChanged)));

        UpdatePreviewAndValidation();
    }

    public string Name
    {
        get => _name;
        set
        {
            var trimmed = value?.Trim() ?? "Paleta";
            if (_name != trimmed)
            {
                _name = trimmed;
                Notify();
                Notify(nameof(DisplayName));
                _onChanged?.Invoke();
            }
        }
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            if (_isEnabled != value)
            {
                _isEnabled = value;
                Notify();
                Notify(nameof(DisplayName));
                _onChanged?.Invoke();
            }
        }
    }

    public string DisplayName => IsEnabled ? _name : $"{_name} (Desactivada)";

    public ObservableCollection<PaletteColorViewModel> PrimaryColors
    {
        get => _primaryColors;
        private set { _primaryColors = value; Notify(); }
    }

    public ObservableCollection<PaletteColorViewModel> SecondaryColors
    {
        get => _secondaryColors;
        private set { _secondaryColors = value; Notify(); }
    }

    public ObservableCollection<Brush> PreviewBrushes
    {
        get => _previewBrushes;
        private set { _previewBrushes = value; Notify(); }
    }

    public bool HasValidationErrors
    {
        get => _hasValidationErrors;
        private set { _hasValidationErrors = value; Notify(); }
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        private set { _validationMessage = value; Notify(); }
    }

    public void OnColorChanged()
    {
        UpdatePreviewAndValidation();
        _onChanged?.Invoke();
    }

    public void UpdatePreviewAndValidation()
    {
        foreach (var c in PrimaryColors)
            c.CanRemove = PrimaryColors.Count > 1;

        foreach (var c in SecondaryColors)
            c.CanRemove = SecondaryColors.Count > 1;

        var brushes = new ObservableCollection<Brush>();
        foreach (var c in PrimaryColors.Where(c => c.IsValid))
            brushes.Add(c.Brush);
        foreach (var c in SecondaryColors.Where(c => c.IsValid))
            brushes.Add(c.Brush);

        PreviewBrushes = brushes;

        var hasSyntaxErrors = PrimaryColors.Concat(SecondaryColors).Any(c => !c.IsValid);
        HasValidationErrors = hasSyntaxErrors;
        ValidationMessage = hasSyntaxErrors ? Strings.APP_PALETTE_INVALID_WARNING : string.Empty;
    }

    public PaletteEntry ToPaletteEntry()
    {
        var primarySk = PrimaryColors
            .Select(c => c.AsSkColor())
            .Where(c => c.HasValue)
            .Select(c => c!.Value)
            .ToList();

        var secondarySk = SecondaryColors
            .Select(c => c.AsSkColor())
            .Where(c => c.HasValue)
            .Select(c => c!.Value)
            .ToList();

        if (primarySk.Count == 0) primarySk = [new SKColor(50, 70, 90)];
        if (secondarySk.Count == 0) secondarySk = [new SKColor(30, 40, 60)];

        return new PaletteEntry(Name, IsEnabled, primarySk.AsReadOnly(), secondarySk.AsReadOnly());
    }

    public PaletteEntryViewModel Clone(string newName, Action? onChanged = null)
    {
        var primarySk = PrimaryColors.Select(c => c.AsSkColor() ?? new SKColor(50, 70, 90));
        var secondarySk = SecondaryColors.Select(c => c.AsSkColor() ?? new SKColor(30, 40, 60));
        return new PaletteEntryViewModel(newName, IsEnabled, primarySk, secondarySk, onChanged ?? _onChanged);
    }

    private void Notify([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
