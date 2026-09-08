using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TodayWallpaper.Core.Generators;
using TodayWallpaper.Core.Localization;
using TodayWallpaper.Core.Palette;
using TodayWallpaper.Core.Settings;

namespace TodayWallpaper.App.ViewModels;

/// <summary>ViewModel for the settings window.</summary>
public sealed class SettingsViewModel : INotifyPropertyChanged
{
    private readonly ISettingsStore _settingsStore;
    private readonly WallpaperGeneratorFactory _generatorFactory;
    private AppSettings _settings = new();

    public SettingsViewModel(
        ISettingsStore settingsStore,
        WallpaperGeneratorFactory generatorFactory)
    {
        _settingsStore = settingsStore;
        _generatorFactory = generatorFactory;
        UpdateGeneratorStyleOptions();
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    // ── Bound properties ─────────────────────────────────────────────────────

    /// <summary>Wallpaper refresh interval in hours (1-24).</summary>
    public int RefreshIntervalHours
    {
        get => _settings.RefreshIntervalHours;
        set { _settings = _settings with { RefreshIntervalHours = value }; Notify(); }
    }

    /// <summary>Palette mode selection.</summary>
    public PaletteMode PaletteMode
    {
        get => _settings.PaletteMode;
        set { _settings = _settings with { PaletteMode = value }; Notify(); }
    }

    /// <summary>Available generator style options with selection state.</summary>
    public ObservableCollection<GeneratorStyleOption> GeneratorStyleOptions { get; } = [];

    private void UpdateGeneratorStyleOptions()
    {
        var selected = _settings.SelectedGeneratorStyles;
        GeneratorStyleOptions.Clear();
        foreach (var styleId in _generatorFactory.StyleIds)
        {
            var isSelected = selected.Contains(styleId, StringComparer.OrdinalIgnoreCase);
            var option = new GeneratorStyleOption(
                styleId,
                Strings.GetGeneratorStyleName(styleId),
                isSelected,
                OnGeneratorStyleOptionChanged);
            GeneratorStyleOptions.Add(option);
        }
    }

    private void SyncGeneratorStyleOptionsFromSettings()
    {
        var selected = _settings.SelectedGeneratorStyles;
        foreach (var option in GeneratorStyleOptions)
        {
            option.IsSelected = selected.Contains(option.StyleId, StringComparer.OrdinalIgnoreCase);
        }
    }

    private void OnGeneratorStyleOptionChanged()
    {
        var selected = GeneratorStyleOptions
            .Where(o => o.IsSelected)
            .Select(o => o.StyleId)
            .ToList();

        _settings = _settings with
        {
            SelectedGeneratorStyles = selected.Count > 0 ? selected : AppSettings.DefaultGeneratorStyles
        };
    }

    /// <summary>Master on/off switch.</summary>
    public bool IsEnabled
    {
        get => _settings.IsEnabled;
        set { _settings = _settings with { IsEnabled = value }; Notify(); }
    }

    /// <summary>Maximum wallpapers kept in history.</summary>
    public int HistoryMaxCount
    {
        get => _settings.HistoryMaxCount;
        set { _settings = _settings with { HistoryMaxCount = value }; Notify(); }
    }

    /// <summary>Available palette modes for a ComboBox.</summary>
    public IEnumerable<PaletteMode> PaletteModes { get; } = Enum.GetValues<PaletteMode>();

    // ── Commands ─────────────────────────────────────────────────────────────

    /// <summary>Loads settings from the store into bound properties.</summary>
    public async Task LoadAsync()
    {
        _settings = await _settingsStore.LoadAsync();
        SyncGeneratorStyleOptionsFromSettings();
        Notify(nameof(RefreshIntervalHours));
        Notify(nameof(PaletteMode));
        Notify(nameof(HistoryMaxCount));
        Notify(nameof(IsEnabled));
        Notify(nameof(GeneratorStyleOptions));
    }

    /// <summary>Persists the current settings to disk.</summary>
    public async Task SaveAsync()
    {
        var selected = GeneratorStyleOptions.Where(o => o.IsSelected).Select(o => o.StyleId).ToList();
        if (selected.Count == 0)
        {
            selected = AppSettings.DefaultGeneratorStyles.ToList();
            SyncGeneratorStyleOptionsFromSettings();
        }
        _settings = _settings with { SelectedGeneratorStyles = selected };
        await _settingsStore.SaveAsync(_settings);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private void Notify([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
