using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using Color = System.Windows.Media.Color;
using SkiaSharp;
using TodayWallpaper.Core.Palette;

namespace TodayWallpaper.App.ViewModels;

/// <summary>
/// Represents a single editable color within a palette entry.
/// </summary>
public sealed class PaletteColorViewModel : INotifyPropertyChanged
{
    private string _hex;
    private SolidColorBrush _brush;
    private bool _isValid = true;
    private bool _isLuminanceAcceptable = true;
    private float _lightnessPercent = 50f;
    private string _luminanceFeedback = string.Empty;
    private bool _canRemove = true;
    private Action? _onChanged;

    public event PropertyChangedEventHandler? PropertyChanged;

    public PaletteColorViewModel(string hex, Action? onChanged = null)
    {
        _hex = hex.StartsWith('#') ? hex.ToUpperInvariant() : $"#{hex.ToUpperInvariant()}";
        _onChanged = onChanged;
        _brush = new SolidColorBrush(Colors.DarkGray);
        UpdateColorState();
    }

    public string Hex
    {
        get => _hex;
        set
        {
            var formatted = value?.Trim() ?? "";
            if (!formatted.StartsWith('#') && formatted.Length > 0)
                formatted = $"#{formatted}";

            if (_hex != formatted)
            {
                _hex = formatted.ToUpperInvariant();
                Notify();
                UpdateColorState();
                _onChanged?.Invoke();
            }
        }
    }

    public SolidColorBrush Brush
    {
        get => _brush;
        private set { _brush = value; Notify(); }
    }

    public bool IsValid
    {
        get => _isValid;
        private set { _isValid = value; Notify(); }
    }

    public bool IsLuminanceAcceptable
    {
        get => _isLuminanceAcceptable;
        private set { _isLuminanceAcceptable = value; Notify(); }
    }

    public float LightnessPercent
    {
        get => _lightnessPercent;
        private set { _lightnessPercent = value; Notify(); }
    }

    public string LuminanceFeedback
    {
        get => _luminanceFeedback;
        private set { _luminanceFeedback = value; Notify(); }
    }

    public bool CanRemove
    {
        get => _canRemove;
        set { _canRemove = value; Notify(); }
    }

    public void SetOnChangedHandler(Action? onChanged)
    {
        _onChanged = onChanged;
    }

    public SKColor? AsSkColor()
    {
        return SKColor.TryParse(_hex, out var color) ? color : null;
    }

    public void SetFromRgb(byte r, byte g, byte b)
    {
        Hex = $"#{r:X2}{g:X2}{b:X2}";
    }

    public PaletteColorViewModel Clone(Action? onChanged = null)
    {
        return new PaletteColorViewModel(_hex, onChanged ?? _onChanged);
    }

    private void UpdateColorState()
    {
        if (SKColor.TryParse(_hex, out var skColor))
        {
            IsValid = true;
            var wpfColor = Color.FromRgb(skColor.Red, skColor.Green, skColor.Blue);
            var brush = new SolidColorBrush(wpfColor);
            brush.Freeze();
            Brush = brush;

            skColor.ToHsl(out _, out _, out float l);
            LightnessPercent = (float)Math.Round(l, 1);
            IsLuminanceAcceptable = LuminanceValidator.IsAcceptable(skColor);

            if (IsLuminanceAcceptable)
            {
                LuminanceFeedback = $"L: {LightnessPercent:0.#}%";
            }
            else if (l < LuminanceValidator.MinLightness)
            {
                LuminanceFeedback = $"L: {LightnessPercent:0.#}% ℹ️ (Rec. ≥ 20%)";
            }
            else
            {
                LuminanceFeedback = $"L: {LightnessPercent:0.#}% ⚠️ (Rec. ≤ 55%)";
            }
        }
        else
        {
            IsValid = false;
            IsLuminanceAcceptable = false;
            LightnessPercent = 0;
            LuminanceFeedback = "Hex inválido";
            var brush = new SolidColorBrush(Colors.Transparent);
            brush.Freeze();
            Brush = brush;
        }
    }

    private void Notify([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
