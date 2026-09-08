using System.ComponentModel;

namespace TodayWallpaper.Core.Generators;

/// <summary>
/// Observable model representing a generator style option for multi-selection UI.
/// </summary>
public sealed class GeneratorStyleOption : INotifyPropertyChanged
{
    private bool _isSelected;
    private readonly Action? _onChanged;

    /// <summary>Gets the unique identifier of the generator style (e.g. "BlurBlobs").</summary>
    public string StyleId { get; }

    /// <summary>Gets the localized human-readable display name for this generator style.</summary>
    public string DisplayName { get; }

    /// <summary>Gets or sets whether this style is selected for wallpaper generation.</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                _onChanged?.Invoke();
            }
        }
    }

    /// <summary>Initializes a new instance of the <see cref="GeneratorStyleOption"/> class.</summary>
    public GeneratorStyleOption(string styleId, string displayName, bool isSelected = true, Action? onChanged = null)
    {
        StyleId = styleId;
        DisplayName = displayName;
        _isSelected = isSelected;
        _onChanged = onChanged;
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;
}
