using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;
using TodayWallpaper.Core.Generators;
using TodayWallpaper.Core.History;
using TodayWallpaper.Core.Localization;
using TodayWallpaper.Core.Palette;
using TodayWallpaper.Core.Settings;
using TodayWallpaper.Core.TaskScheduler;
using TodayWallpaper.Core.Wallpaper;
using TodayWallpaper.Core.Weather;

using System.Windows.Media;
using Brush = System.Windows.Media.Brush;
using SkiaSharp;

namespace TodayWallpaper.App.ViewModels;

/// <summary>
/// Main view model powering the primary management window of TodayWallpaper.
/// </summary>
public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly ISettingsStore _settingsStore;
    private readonly IHistoryStore _historyStore;
    private readonly IWallpaperSetter _wallpaperSetter;
    private readonly WallpaperPipeline _pipeline;
    private readonly IGeolocationService _geolocationService;
    private readonly IWeatherService _weatherService;
    private readonly WallpaperGeneratorFactory _generatorFactory;
    private readonly TaskSchedulerRegistration _taskSchedulerRegistration;
    private readonly IPaletteConfigLoader _paletteConfigLoader;
    private readonly ILogger<MainViewModel> _logger;

    private AppSettings _settings = new();
    private bool _isBusy;
    private string _statusMessage = Strings.VM_STATUS_DEFAULT;
    private string _detectedLocationText = Strings.VM_LOCATION_DEFAULT;

    private CancellationTokenSource? _settingsDebounceCts;
    private CancellationTokenSource? _palettesDebounceCts;
    private bool _isInitializing;

    private ObservableCollection<HistoryItemViewModel> _historyItems = [];
    private HistoryItemViewModel? _selectedHistoryItem;

    public event PropertyChangedEventHandler? PropertyChanged;

    public MainViewModel(
        ISettingsStore settingsStore,
        IHistoryStore historyStore,
        IWallpaperSetter wallpaperSetter,
        WallpaperPipeline pipeline,
        IGeolocationService geolocationService,
        IWeatherService weatherService,
        WallpaperGeneratorFactory generatorFactory,
        TaskSchedulerRegistration taskSchedulerRegistration,
        IPaletteConfigLoader paletteConfigLoader,
        ILogger<MainViewModel> logger)
    {
        _settingsStore = settingsStore;
        _historyStore = historyStore;
        _wallpaperSetter = wallpaperSetter;
        _pipeline = pipeline;
        _geolocationService = geolocationService;
        _weatherService = weatherService;
        _generatorFactory = generatorFactory;
        _taskSchedulerRegistration = taskSchedulerRegistration;
        _paletteConfigLoader = paletteConfigLoader;
        _logger = logger;
        UpdateGeneratorStyleOptions();
    }

    // ── General State ────────────────────────────────────────────────────────

    public bool IsBusy
    {
        get => _isBusy;
        set { _isBusy = value; Notify(); Notify(nameof(IsNotBusy)); }
    }

    public bool IsNotBusy => !IsBusy;

    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; Notify(); }
    }

    // ── Scheduling Properties ────────────────────────────────────────────────

    public bool IsEnabled
    {
        get => _settings.IsEnabled;
        set
        {
            _settings = _settings with { IsEnabled = value };
            Notify();
            Notify(nameof(ScheduleStatusSummary));
            TriggerAutoSaveSettings();
        }
    }

    public ScheduleMode ScheduleMode
    {
        get => _settings.ScheduleMode;
        set
        {
            _settings = _settings with { ScheduleMode = value };
            Notify();
            Notify(nameof(IsIntervalMode));
            Notify(nameof(IsDailyMode));
            Notify(nameof(ScheduleStatusSummary));
            TriggerAutoSaveSettings();
        }
    }

    public bool IsIntervalMode
    {
        get => ScheduleMode == ScheduleMode.Interval;
        set { if (value) ScheduleMode = ScheduleMode.Interval; }
    }

    public bool IsDailyMode
    {
        get => ScheduleMode == ScheduleMode.Daily;
        set { if (value) ScheduleMode = ScheduleMode.Daily; }
    }

    public int RefreshIntervalHours
    {
        get => _settings.RefreshIntervalHours;
        set
        {
            _settings = _settings with { RefreshIntervalHours = Math.Clamp(value, 1, 24) };
            Notify();
            Notify(nameof(ScheduleStatusSummary));
            TriggerAutoSaveSettings();
        }
    }

    public int DailyAtHour
    {
        get => _settings.DailyAtHour;
        set
        {
            _settings = _settings with { DailyAtHour = Math.Clamp(value, 0, 23) };
            Notify();
            Notify(nameof(ScheduleStatusSummary));
            TriggerAutoSaveSettings();
        }
    }

    public int DailyAtMinute
    {
        get => _settings.DailyAtMinute;
        set
        {
            _settings = _settings with { DailyAtMinute = Math.Clamp(value, 0, 59) };
            Notify();
            Notify(nameof(ScheduleStatusSummary));
            TriggerAutoSaveSettings();
        }
    }

    public bool RunOnStartupIfExpired
    {
        get => _settings.RunOnStartupIfExpired;
        set
        {
            _settings = _settings with { RunOnStartupIfExpired = value };
            Notify();
            Notify(nameof(ScheduleStatusSummary));
            TriggerAutoSaveSettings();
        }
    }

    public string ScheduleStatusSummary
    {
        get
        {
            if (!IsEnabled) return Strings.VM_SCHEDULE_SUMMARY_PAUSED;
            var startupNote = RunOnStartupIfExpired ? Strings.VM_SCHEDULE_SUMMARY_STARTUP_NOTE : "";
            if (ScheduleMode == ScheduleMode.Interval)
                return Strings.Format(Strings.VM_SCHEDULE_SUMMARY_INTERVAL_FORMAT, RefreshIntervalHours, startupNote);
            else
                return Strings.Format(Strings.VM_SCHEDULE_SUMMARY_DAILY_FORMAT, DailyAtHour, DailyAtMinute, startupNote);
        }
    }

    public string LastGenerationText
    {
        get
        {
            if (_settings.LastGenerationTimeUtc is null) return Strings.VM_LAST_GEN_NEVER;
            var local = _settings.LastGenerationTimeUtc.Value.ToLocalTime();
            var diff = DateTime.Now - local;
            var ago = diff.TotalHours >= 24
                ? Strings.Format(Strings.VM_AGO_DAYS_FORMAT, Math.Floor(diff.TotalDays))
                : diff.TotalMinutes >= 60
                    ? Strings.Format(Strings.VM_AGO_HOURS_FORMAT, Math.Floor(diff.TotalHours))
                    : Strings.Format(Strings.VM_AGO_MINS_FORMAT, Math.Floor(diff.TotalMinutes));
            return $"{local:dd/MM/yyyy HH:mm} ({ago})";
        }
    }

    // ── Generation & Style Properties ────────────────────────────────────────

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
        TriggerAutoSaveSettings();
    }

    public PaletteMode PaletteMode
    {
        get => _settings.PaletteMode;
        set
        {
            _settings = _settings with { PaletteMode = value };
            Notify();
            TriggerAutoSaveSettings();
        }
    }

    public IEnumerable<PaletteMode> PaletteModes { get; } = Enum.GetValues<PaletteMode>();

    public int HistoryMaxCount
    {
        get => _settings.HistoryMaxCount;
        set
        {
            _settings = _settings with { HistoryMaxCount = Math.Clamp(value, 10, 200) };
            Notify();
            TriggerAutoSaveSettings();
        }
    }

    // ── Location Properties ──────────────────────────────────────────────────

    public bool IsAutoLocation
    {
        get => _settings.Location is null;
        set
        {
            if (value)
            {
                _settings = _settings with { Location = null };
                Notify();
                Notify(nameof(IsManualLocation));
                TriggerAutoSaveSettings();
            }
        }
    }

    public bool IsManualLocation
    {
        get => _settings.Location is not null;
        set
        {
            if (value && _settings.Location is null)
            {
                _settings = _settings with { Location = new LocationSettings(40.4168, -3.7038, "Madrid") };
                Notify();
                Notify(nameof(IsAutoLocation));
                Notify(nameof(ManualLatitude));
                Notify(nameof(ManualLongitude));
                Notify(nameof(ManualCity));
                TriggerAutoSaveSettings();
            }
        }
    }

    public double ManualLatitude
    {
        get => _settings.Location?.Latitude ?? 0.0;
        set
        {
            if (_settings.Location is not null)
            {
                _settings = _settings with { Location = _settings.Location with { Latitude = value } };
                Notify();
                TriggerAutoSaveSettings();
            }
        }
    }

    public double ManualLongitude
    {
        get => _settings.Location?.Longitude ?? 0.0;
        set
        {
            if (_settings.Location is not null)
            {
                _settings = _settings with { Location = _settings.Location with { Longitude = value } };
                Notify();
                TriggerAutoSaveSettings();
            }
        }
    }

    public string ManualCity
    {
        get => _settings.Location?.City ?? "";
        set
        {
            if (_settings.Location is not null)
            {
                _settings = _settings with { Location = _settings.Location with { City = value } };
                Notify();
                TriggerAutoSaveSettings();
            }
        }
    }

    public string DetectedLocationText
    {
        get => _detectedLocationText;
        private set { _detectedLocationText = value; Notify(); }
    }

    // ── Weather Summary Properties ───────────────────────────────────────────

    public WeatherData? CachedWeather => _settings.CachedWeatherData;

    public bool HasWeather => CachedWeather is not null;

    public string WeatherSummaryTitle => CachedWeather is not null
        ? $"{CachedWeather.LocationName}"
        : Strings.VM_WEATHER_NO_DATA;

    public string WeatherConditionText => CachedWeather is not null
        ? $"{GetWeatherIcon(CachedWeather.Condition)} {CachedWeather.Condition}"
        : "—";

    public string WeatherTemperatureText => CachedWeather is not null
        ? Strings.Format(Strings.VM_WEATHER_TEMP_FORMAT, CachedWeather.TemperatureCelsius, CachedWeather.ApparentTemperatureCelsius)
        : "—";

    public string WeatherWindText => CachedWeather is not null
        ? $"{CachedWeather.WindspeedKmh:F0} km/h"
        : "—";

    public string WeatherHumidityText => CachedWeather is not null
        ? $"{CachedWeather.Humidity:F0} %"
        : "—";

    public string WeatherPrecipitationText => CachedWeather is not null
        ? $"{CachedWeather.PrecipitationMm:F1} mm"
        : "—";

    public string WeatherCloudCoverText => CachedWeather is not null
        ? $"{CachedWeather.CloudCoverPercent:F0} %"
        : "—";

    public string WeatherVisibilityText => CachedWeather is not null
        ? $"{CachedWeather.VisibilityKm:F1} km"
        : "—";

    // ── History Properties ───────────────────────────────────────────────────

    public ObservableCollection<HistoryItemViewModel> HistoryItems
    {
        get => _historyItems;
        private set { _historyItems = value; Notify(); }
    }

    public HistoryItemViewModel? SelectedHistoryItem
    {
        get => _selectedHistoryItem;
        set
        {
            _selectedHistoryItem = value;
            Notify();
            Notify(nameof(HasSelectedHistoryItem));
        }
    }

    public bool HasSelectedHistoryItem => SelectedHistoryItem is not null;

    // ── Palette Configuration Properties ─────────────────────────────────────

    private PaletteConfig? _paletteConfig;
    private PaletteModeOption? _selectedPaletteEditModeOption;
    private WeatherConditionOption? _selectedPaletteWeatherConditionOption;
    private ObservableCollection<PaletteEntryViewModel> _activePalettes = [];
    private PaletteEntryViewModel? _selectedPalette;

    public IReadOnlyList<WeatherConditionOption> WeatherConditionOptions { get; } =
    [
        new(WeatherCondition.Sunny, Strings.WEATHER_COND_SUNNY, "☀️"),
        new(WeatherCondition.Cloudy, Strings.WEATHER_COND_CLOUDY, "☁️"),
        new(WeatherCondition.Rainy, Strings.WEATHER_COND_RAINY, "🌧️"),
        new(WeatherCondition.Stormy, Strings.WEATHER_COND_STORMY, "⛈️"),
        new(WeatherCondition.Snowy, Strings.WEATHER_COND_SNOWY, "❄️"),
        new(WeatherCondition.Foggy, Strings.WEATHER_COND_FOGGY, "🌫️")
    ];

    public IReadOnlyList<PaletteModeOption> PaletteModeOptions { get; } =
    [
        new(PaletteMode.Empathic, Strings.PALETTE_MODE_EMPATHIC),
        new(PaletteMode.Contrast, Strings.PALETTE_MODE_CONTRAST)
    ];

    public PaletteModeOption? SelectedPaletteEditModeOption
    {
        get => _selectedPaletteEditModeOption ?? PaletteModeOptions[0];
        set
        {
            if (_selectedPaletteEditModeOption != value && value is not null)
            {
                PersistActiveColorsToLocalConfig();
                TriggerAutoSavePalettes(delayMs: 0);
                _selectedPaletteEditModeOption = value;
                Notify();
                LoadActivePalettesForCurrentSelection();
            }
        }
    }

    public WeatherConditionOption? SelectedPaletteWeatherConditionOption
    {
        get => _selectedPaletteWeatherConditionOption ?? WeatherConditionOptions[0];
        set
        {
            if (_selectedPaletteWeatherConditionOption != value && value is not null)
            {
                PersistActiveColorsToLocalConfig();
                TriggerAutoSavePalettes(delayMs: 0);
                _selectedPaletteWeatherConditionOption = value;
                Notify();
                LoadActivePalettesForCurrentSelection();
            }
        }
    }

    public ObservableCollection<PaletteEntryViewModel> ActivePalettes
    {
        get => _activePalettes;
        private set { _activePalettes = value; Notify(); }
    }

    public PaletteEntryViewModel? SelectedPalette
    {
        get => _selectedPalette;
        set
        {
            if (_selectedPalette != value)
            {
                _selectedPalette = value;
                Notify();
                Notify(nameof(HasSelectedPalette));
                Notify(nameof(CanDeletePalette));
            }
        }
    }

    public bool HasSelectedPalette => SelectedPalette is not null;

    public bool CanDeletePalette => ActivePalettes.Count > 1;

    // ── Lifecycle & Initialization ───────────────────────────────────────────

    public async Task InitializeAsync()
    {
        try
        {
            _isInitializing = true;
            IsBusy = true;
            StatusMessage = Strings.VM_STATUS_INIT_LOADING;

            _settings = await _settingsStore.LoadAsync();
            NotifyAllSettings();

            if (_settings.CachedLocation is not null)
            {
                DetectedLocationText = $"{_settings.CachedLocation.City}, {_settings.CachedLocation.Country} ({_settings.CachedLocation.Latitude:F2}, {_settings.CachedLocation.Longitude:F2})";
            }

            await LoadHistoryAsync();
            LoadPalettes();

            // Check if startup expiration triggers generation
            if (_settings.IsEnabled && _settings.RunOnStartupIfExpired && _settings.IsExpired())
            {
                _logger.LogInformation("Wallpaper is expired on startup. Generating fresh wallpaper...");
                StatusMessage = Strings.VM_STATUS_INIT_EXPIRED_GEN;
                await _pipeline.RunAsync(force: true);
                _settings = await _settingsStore.LoadAsync();
                NotifyAllSettings();
                await LoadHistoryAsync();
            }

            StatusMessage = Strings.VM_STATUS_DEFAULT;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize MainViewModel.");
            StatusMessage = Strings.Format(Strings.VM_STATUS_INIT_ERROR, ex.Message);
        }
        finally
        {
            IsBusy = false;
            _isInitializing = false;
        }
    }

    // ── Auto-Save & Debounce Mechanisms ──────────────────────────────────────

    private void SetStatus(string message)
    {
        if (System.Windows.Application.Current?.Dispatcher is { } dispatcher && !dispatcher.CheckAccess())
        {
            dispatcher.BeginInvoke(() => StatusMessage = message);
        }
        else
        {
            StatusMessage = message;
        }
    }

    public void TriggerAutoSaveSettings(int delayMs = 500)
    {
        if (_isInitializing) return;

        _settingsDebounceCts?.Cancel();
        _settingsDebounceCts?.Dispose();
        _settingsDebounceCts = new CancellationTokenSource();
        var token = _settingsDebounceCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delayMs, token);
                if (token.IsCancellationRequested) return;

                await SaveSettingsInternalAsync(showStatusMessage: true);
            }
            catch (OperationCanceledException)
            {
                // Debounce superseded
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed during debounced auto-save of settings.");
            }
        }, token);
    }

    public void TriggerAutoSavePalettes(int delayMs = 500)
    {
        if (_isInitializing) return;

        _palettesDebounceCts?.Cancel();
        _palettesDebounceCts?.Dispose();
        _palettesDebounceCts = new CancellationTokenSource();
        var token = _palettesDebounceCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(delayMs, token);
                if (token.IsCancellationRequested) return;

                await SavePalettesInternalAsync(showStatusMessage: true);
            }
            catch (OperationCanceledException)
            {
                // Debounce superseded
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed during debounced auto-save of palettes.");
            }
        }, token);
    }

    public async Task SaveSettingsInternalAsync(bool showStatusMessage = true)
    {
        try
        {
            if (showStatusMessage) SetStatus(Strings.VM_STATUS_AUTOSAVING);

            var selected = GeneratorStyleOptions.Where(o => o.IsSelected).Select(o => o.StyleId).ToList();
            if (selected.Count == 0)
            {
                selected = AppSettings.DefaultGeneratorStyles.ToList();
            }
            _settings = _settings with { SelectedGeneratorStyles = selected };

            await _settingsStore.SaveAsync(_settings);

            // Register or unregister Windows Task Scheduler
            var workerPath = ResolveWorkerPath();
            if (_settings.IsEnabled && File.Exists(workerPath))
            {
                _taskSchedulerRegistration.Register(workerPath, _settings);
            }
            else if (!_settings.IsEnabled)
            {
                _taskSchedulerRegistration.Unregister();
            }

            if (showStatusMessage)
            {
                SetStatus(Strings.VM_STATUS_AUTOSAVED);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-save settings.");
            SetStatus(Strings.Format(Strings.VM_STATUS_SAVE_ERROR, ex.Message));
        }
    }

    public async Task SavePalettesInternalAsync(bool showStatusMessage = true)
    {
        try
        {
            if (showStatusMessage) SetStatus(Strings.VM_STATUS_AUTOSAVING);

            PersistActiveColorsToLocalConfig();

            if (_paletteConfig is not null)
            {
                _paletteConfigLoader.Save(_paletteConfig);
                if (showStatusMessage)
                {
                    SetStatus(Strings.VM_STATUS_AUTOSAVED);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-save palettes.");
            SetStatus(Strings.Format(Strings.VM_STATUS_SAVE_ERROR, ex.Message));
        }
        await Task.CompletedTask;
    }

    public async Task FlushPendingSavesAsync()
    {
        if (_settingsDebounceCts is not null && !_settingsDebounceCts.IsCancellationRequested)
        {
            _settingsDebounceCts.Cancel();
            await SaveSettingsInternalAsync(showStatusMessage: false);
        }
        if (_palettesDebounceCts is not null && !_palettesDebounceCts.IsCancellationRequested)
        {
            _palettesDebounceCts.Cancel();
            await SavePalettesInternalAsync(showStatusMessage: false);
        }
    }

    public async Task SaveSettingsAsync() => await SaveSettingsInternalAsync(showStatusMessage: true);

    public async Task SavePalettesAsync() => await SavePalettesInternalAsync(showStatusMessage: true);

    public async Task DetectLocationAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = Strings.VM_STATUS_DETECT_IP_PROGRESS;

            var loc = await _geolocationService.GetLocationAsync();
            DetectedLocationText = $"{loc.City}, {loc.Country} ({loc.Latitude:F2}, {loc.Longitude:F2})";

            if (IsManualLocation)
            {
                ManualLatitude = loc.Latitude;
                ManualLongitude = loc.Longitude;
                ManualCity = loc.City;
            }

            _settings = _settings with { CachedLocation = loc };
            await _settingsStore.SaveAsync(_settings);

            StatusMessage = Strings.Format(Strings.VM_STATUS_DETECT_IP_SUCCESS, loc.City, loc.Country);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect location by IP.");
            StatusMessage = Strings.Format(Strings.VM_STATUS_DETECT_IP_ERROR, ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RefreshWeatherAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = Strings.VM_STATUS_WEATHER_PROGRESS;

            double lat = IsManualLocation ? ManualLatitude : (_settings.CachedLocation?.Latitude ?? 40.4168);
            double lon = IsManualLocation ? ManualLongitude : (_settings.CachedLocation?.Longitude ?? -3.7038);
            string city = IsManualLocation ? ManualCity : (_settings.CachedLocation?.City ?? "Actual");

            if (IsAutoLocation && _settings.CachedLocation is null)
            {
                var loc = await _geolocationService.GetLocationAsync();
                lat = loc.Latitude;
                lon = loc.Longitude;
                city = loc.City;
                _settings = _settings with { CachedLocation = loc };
            }

            var weather = await _weatherService.GetCurrentWeatherAsync(lat, lon, city);
            _settings = _settings with { CachedWeatherData = weather };
            await _settingsStore.SaveAsync(_settings);

            NotifyAllWeather();
            StatusMessage = Strings.Format(Strings.VM_STATUS_WEATHER_SUCCESS, weather.Condition, weather.TemperatureCelsius, weather.LocationName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh weather.");
            StatusMessage = Strings.Format(Strings.VM_STATUS_WEATHER_ERROR, ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task GenerateNowAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = Strings.VM_STATUS_GEN_PROGRESS;

            var success = await _pipeline.RunAsync(force: true);
            if (success)
            {
                _settings = await _settingsStore.LoadAsync();
                NotifyAllSettings();
                NotifyAllWeather();
                await LoadHistoryAsync();
                StatusMessage = Strings.VM_STATUS_GEN_SUCCESS;
            }
            else
            {
                StatusMessage = Strings.VM_STATUS_GEN_INCOMPLETE;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate wallpaper now.");
            StatusMessage = Strings.Format(Strings.VM_STATUS_GEN_ERROR, ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void ApplySelectedWallpaper()
    {
        if (SelectedHistoryItem is null) return;

        try
        {
            if (!File.Exists(SelectedHistoryItem.FullPath))
            {
                StatusMessage = Strings.VM_STATUS_APPLY_NOT_FOUND;
                return;
            }

            _wallpaperSetter.Set(SelectedHistoryItem.FullPath);
            StatusMessage = Strings.Format(Strings.VM_STATUS_APPLY_SUCCESS, SelectedHistoryItem.DisplayTitle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply wallpaper.");
            StatusMessage = Strings.Format(Strings.VM_STATUS_APPLY_ERROR, ex.Message);
        }
    }

    public async Task TogglePinSelectedAsync()
    {
        if (SelectedHistoryItem is null) return;

        try
        {
            var newPinned = !SelectedHistoryItem.Pinned;
            await _historyStore.SetPinnedAsync(SelectedHistoryItem.Id, newPinned);
            SelectedHistoryItem.Pinned = newPinned;
            StatusMessage = newPinned ? Strings.VM_STATUS_PIN_PINNED : Strings.VM_STATUS_PIN_UNPINNED;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to toggle pin.");
            StatusMessage = Strings.Format(Strings.VM_STATUS_PIN_ERROR, ex.Message);
        }
    }

    public async Task DeleteSelectedAsync()
    {
        if (SelectedHistoryItem is null) return;
        if (SelectedHistoryItem.Pinned)
        {
            StatusMessage = Strings.VM_STATUS_DELETE_PINNED_BLOCKED;
            return;
        }

        try
        {
            await _historyStore.DeleteAsync(SelectedHistoryItem.Id);
            HistoryItems.Remove(SelectedHistoryItem);
            SelectedHistoryItem = HistoryItems.FirstOrDefault();
            StatusMessage = Strings.VM_STATUS_DELETE_SUCCESS;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete history item.");
            StatusMessage = Strings.Format(Strings.VM_STATUS_DELETE_ERROR, ex.Message);
        }
    }

    public void OpenSelectedInViewer()
    {
        if (SelectedHistoryItem is null || !File.Exists(SelectedHistoryItem.FullPath)) return;

        try
        {
            Process.Start(new ProcessStartInfo(SelectedHistoryItem.FullPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open image in external viewer.");
        }
    }

    // ── Helper Methods ───────────────────────────────────────────────────────

    private async Task LoadHistoryAsync()
    {
        var list = await _historyStore.GetAllAsync();
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var baseDir = Path.Combine(appData, "TodayWallpaper");

        var items = new ObservableCollection<HistoryItemViewModel>();
        foreach (var entry in list)
        {
            var fullPath = Path.Combine(baseDir, entry.File);
            items.Add(new HistoryItemViewModel(entry, fullPath));
        }

        HistoryItems = items;
        SelectedHistoryItem = items.FirstOrDefault();
    }

    private void NotifyAllSettings()
    {
        Notify(nameof(IsEnabled));
        Notify(nameof(ScheduleMode));
        Notify(nameof(IsIntervalMode));
        Notify(nameof(IsDailyMode));
        Notify(nameof(RefreshIntervalHours));
        Notify(nameof(DailyAtHour));
        Notify(nameof(DailyAtMinute));
        Notify(nameof(RunOnStartupIfExpired));
        Notify(nameof(ScheduleStatusSummary));
        Notify(nameof(LastGenerationText));
        SyncGeneratorStyleOptionsFromSettings();
        Notify(nameof(GeneratorStyleOptions));
        Notify(nameof(PaletteMode));
        Notify(nameof(HistoryMaxCount));
        Notify(nameof(IsAutoLocation));
        Notify(nameof(IsManualLocation));
        Notify(nameof(ManualLatitude));
        Notify(nameof(ManualLongitude));
        Notify(nameof(ManualCity));
    }

    private void NotifyAllWeather()
    {
        Notify(nameof(CachedWeather));
        Notify(nameof(HasWeather));
        Notify(nameof(WeatherSummaryTitle));
        Notify(nameof(WeatherConditionText));
        Notify(nameof(WeatherTemperatureText));
        Notify(nameof(WeatherWindText));
        Notify(nameof(WeatherHumidityText));
        Notify(nameof(WeatherPrecipitationText));
        Notify(nameof(WeatherCloudCoverText));
        Notify(nameof(WeatherVisibilityText));
    }

    private static string GetWeatherIcon(WeatherCondition condition) => condition switch
    {
        WeatherCondition.Sunny => "☀️",
        WeatherCondition.Cloudy => "⛅",
        WeatherCondition.Rainy => "🌧️",
        WeatherCondition.Stormy => "⛈️",
        WeatherCondition.Snowy => "❄️",
        WeatherCondition.Foggy => "🌫️",
        _ => "🌤️"
    };

    private static string ResolveWorkerPath()
    {
        var appDir = AppContext.BaseDirectory;
        var direct = Path.Combine(appDir, "TodayWallpaper.Worker.exe");
        if (File.Exists(direct)) return direct;

        // Visual Studio / dotnet run debug path
        var debugWorker = Path.GetFullPath(Path.Combine(appDir, @"..\..\..\..\TodayWallpaper.Worker\bin\Debug\net10.0-windows\TodayWallpaper.Worker.exe"));
        if (File.Exists(debugWorker)) return debugWorker;

        return direct;
    }

    // ── Palette Configuration Methods ────────────────────────────────────────

    public void LoadPalettes()
    {
        try
        {
            _paletteConfig = _paletteConfigLoader.Load();
            LoadActivePalettesForCurrentSelection();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load palettes.");
        }
    }

    private void LoadActivePalettesForCurrentSelection()
    {
        _paletteConfig ??= _paletteConfigLoader.Load();

        var editMode = SelectedPaletteEditModeOption?.Mode ?? PaletteMode.Empathic;
        var weatherCond = SelectedPaletteWeatherConditionOption?.Condition ?? WeatherCondition.Sunny;

        var section = editMode == PaletteMode.Empathic
            ? _paletteConfig.Empathic
            : _paletteConfig.Contrast;

        var entries = section.TryGetValue(weatherCond, out var found) && found.Count > 0
            ? found
            : [.. (editMode == PaletteMode.Empathic ? _paletteConfigLoader.GetDefaultConfig().Empathic : _paletteConfigLoader.GetDefaultConfig().Contrast)[weatherCond]];

        var vms = new ObservableCollection<PaletteEntryViewModel>(
            entries.Select(e => new PaletteEntryViewModel(
                e.Name,
                e.IsEnabled,
                e.Primary,
                e.Secondary,
                OnPaletteEntryChanged)));

        ActivePalettes = vms;
        SelectedPalette = vms.FirstOrDefault();
        Notify(nameof(CanDeletePalette));
    }

    private void PersistActiveColorsToLocalConfig()
    {
        if (_paletteConfig is null || ActivePalettes.Count == 0) return;

        var editMode = SelectedPaletteEditModeOption?.Mode ?? PaletteMode.Empathic;
        var weatherCond = SelectedPaletteWeatherConditionOption?.Condition ?? WeatherCondition.Sunny;

        var entries = ActivePalettes.Select(p => p.ToPaletteEntry()).ToList();

        if (editMode == PaletteMode.Empathic)
        {
            _paletteConfig.Empathic[weatherCond] = entries;
        }
        else
        {
            _paletteConfig.Contrast[weatherCond] = entries;
        }
    }

    private void OnPaletteEntryChanged()
    {
        SelectedPalette?.UpdatePreviewAndValidation();
        TriggerAutoSavePalettes();
    }

    public void AddNewPalette()
    {
        var count = ActivePalettes.Count + 1;
        var newVm = new PaletteEntryViewModel(
            $"Paleta {count}",
            true,
            [new SKColor(40, 80, 110), new SKColor(50, 100, 130)],
            [new SKColor(30, 60, 80), new SKColor(70, 110, 140)],
            OnPaletteEntryChanged);

        ActivePalettes.Add(newVm);
        SelectedPalette = newVm;
        Notify(nameof(CanDeletePalette));
        PersistActiveColorsToLocalConfig();
        TriggerAutoSavePalettes();
        StatusMessage = Strings.APP_STATUS_PALETTE_ADDED;
    }

    public void DuplicateCurrentPalette()
    {
        if (SelectedPalette is null) return;

        var clone = SelectedPalette.Clone($"{SelectedPalette.Name} (Copia)", OnPaletteEntryChanged);
        ActivePalettes.Add(clone);
        SelectedPalette = clone;
        Notify(nameof(CanDeletePalette));
        PersistActiveColorsToLocalConfig();
        TriggerAutoSavePalettes();
        StatusMessage = Strings.APP_STATUS_PALETTE_DUPLICATED;
    }

    public void DeleteCurrentPalette()
    {
        if (SelectedPalette is null || ActivePalettes.Count <= 1) return;

        var index = ActivePalettes.IndexOf(SelectedPalette);
        ActivePalettes.Remove(SelectedPalette);
        SelectedPalette = ActivePalettes.ElementAtOrDefault(Math.Max(0, index - 1)) ?? ActivePalettes.FirstOrDefault();
        Notify(nameof(CanDeletePalette));
        PersistActiveColorsToLocalConfig();
        TriggerAutoSavePalettes();
        StatusMessage = Strings.APP_STATUS_PALETTE_DELETED;
    }

    public void AddPrimaryColor()
    {
        if (SelectedPalette is null) return;
        var nextColor = "#3A5C75";
        var item = new PaletteColorViewModel(nextColor, OnPaletteEntryChanged);
        SelectedPalette.PrimaryColors.Add(item);
        SelectedPalette.UpdatePreviewAndValidation();
        TriggerAutoSavePalettes();
    }

    public void RemovePrimaryColor(PaletteColorViewModel? item)
    {
        if (SelectedPalette is not null && item is not null && SelectedPalette.PrimaryColors.Count > 1 && SelectedPalette.PrimaryColors.Contains(item))
        {
            SelectedPalette.PrimaryColors.Remove(item);
            SelectedPalette.UpdatePreviewAndValidation();
            TriggerAutoSavePalettes();
        }
    }

    public void AddSecondaryColor()
    {
        if (SelectedPalette is null) return;
        var nextColor = "#2E4B5E";
        var item = new PaletteColorViewModel(nextColor, OnPaletteEntryChanged);
        SelectedPalette.SecondaryColors.Add(item);
        SelectedPalette.UpdatePreviewAndValidation();
        TriggerAutoSavePalettes();
    }

    public void RemoveSecondaryColor(PaletteColorViewModel? item)
    {
        if (SelectedPalette is not null && item is not null && SelectedPalette.SecondaryColors.Count > 1 && SelectedPalette.SecondaryColors.Contains(item))
        {
            SelectedPalette.SecondaryColors.Remove(item);
            SelectedPalette.UpdatePreviewAndValidation();
            TriggerAutoSavePalettes();
        }
    }

    public void MoveColor(PaletteColorViewModel? item, bool toSecondary)
    {
        if (SelectedPalette is null || item is null) return;

        if (toSecondary && SelectedPalette.PrimaryColors.Contains(item))
        {
            if (SelectedPalette.PrimaryColors.Count <= 1) return; // Keep at least 1 primary color
            SelectedPalette.PrimaryColors.Remove(item);
            item.SetOnChangedHandler(OnPaletteEntryChanged);
            SelectedPalette.SecondaryColors.Add(item);
            SelectedPalette.UpdatePreviewAndValidation();
            TriggerAutoSavePalettes();
        }
        else if (!toSecondary && SelectedPalette.SecondaryColors.Contains(item))
        {
            if (SelectedPalette.SecondaryColors.Count <= 1) return; // Keep at least 1 secondary color
            SelectedPalette.SecondaryColors.Remove(item);
            item.SetOnChangedHandler(OnPaletteEntryChanged);
            SelectedPalette.PrimaryColors.Add(item);
            SelectedPalette.UpdatePreviewAndValidation();
            TriggerAutoSavePalettes();
        }
    }

    public void ReorderColor(PaletteColorViewModel? item, int targetIndex, bool inSecondary)
    {
        if (SelectedPalette is null || item is null) return;

        var collection = inSecondary ? SelectedPalette.SecondaryColors : SelectedPalette.PrimaryColors;
        if (!collection.Contains(item)) return;

        int oldIndex = collection.IndexOf(item);
        if (oldIndex < 0) return;

        targetIndex = Math.Clamp(targetIndex, 0, collection.Count - 1);
        if (oldIndex != targetIndex)
        {
            collection.Move(oldIndex, targetIndex);
            SelectedPalette.UpdatePreviewAndValidation();
            TriggerAutoSavePalettes();
        }
    }

    public void PickColor(PaletteColorViewModel? item)
    {
        if (item is null) return;

        using var dialog = new System.Windows.Forms.ColorDialog();
        if (item.AsSkColor() is { } currentSk)
        {
            dialog.Color = System.Drawing.Color.FromArgb(currentSk.Red, currentSk.Green, currentSk.Blue);
        }
        dialog.FullOpen = true;

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            item.SetFromRgb(dialog.Color.R, dialog.Color.G, dialog.Color.B);
            SelectedPalette?.UpdatePreviewAndValidation();
            TriggerAutoSavePalettes();
        }
    }

    public void ResetCurrentConditionPalettes()
    {
        var editMode = SelectedPaletteEditModeOption?.Mode ?? PaletteMode.Empathic;
        var weatherCond = SelectedPaletteWeatherConditionOption?.Condition ?? WeatherCondition.Sunny;

        var def = _paletteConfigLoader.GetDefaultConfig();
        var defEntries = (editMode == PaletteMode.Empathic ? def.Empathic : def.Contrast)[weatherCond];

        if (_paletteConfig is not null)
        {
            if (editMode == PaletteMode.Empathic)
                _paletteConfig.Empathic[weatherCond] = [.. defEntries];
            else
                _paletteConfig.Contrast[weatherCond] = [.. defEntries];
        }

        LoadActivePalettesForCurrentSelection();
        StatusMessage = Strings.APP_PALETTE_RESET_MSG;
        TriggerAutoSavePalettes(delayMs: 0);
    }

    public void ResetAllPalettes()
    {
        _paletteConfigLoader.ResetToDefaults();
        _paletteConfig = _paletteConfigLoader.Load();
        LoadActivePalettesForCurrentSelection();
        StatusMessage = Strings.APP_PALETTE_RESET_MSG;
        TriggerAutoSavePalettes(delayMs: 0);
    }

    private void Notify([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>
/// Display option item for weather condition selection in UI.
/// </summary>
public sealed record WeatherConditionOption(WeatherCondition Condition, string DisplayName, string Icon)
{
    public string FullTitle => $"{Icon}  {DisplayName}";
}

/// <summary>
/// Display option item for palette mode selection in UI.
/// </summary>
public sealed record PaletteModeOption(PaletteMode Mode, string DisplayName);

/// <summary>
/// ViewModel wrapping a single history entry with thumbnail image cache.
/// </summary>
public sealed class HistoryItemViewModel : INotifyPropertyChanged
{
    private bool _pinned;
    private BitmapImage? _thumbnail;

    public HistoryEntry Entry { get; }
    public string FullPath { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public HistoryItemViewModel(HistoryEntry entry, string fullPath)
    {
        Entry = entry;
        FullPath = fullPath;
        _pinned = entry.Pinned;
    }

    public string Id => Entry.Id;
    public string Style => Entry.Style;
    public WeatherCondition Condition => Entry.Condition;
    public double Temperature => Entry.Temperature;

    public bool Pinned
    {
        get => _pinned;
        set { _pinned = value; Notify(); }
    }

    public string DisplayTitle
    {
        get
        {
            if (DateTime.TryParse(Entry.Id, out var dt))
                return dt.ToLocalTime().ToString("ddd dd MMM yyyy HH:mm");
            return Entry.Id;
        }
    }

    public string DisplaySubtitle => $"{Entry.Style} · {Entry.Temperature:F1}°C · {Entry.Condition}";

    public BitmapImage? Thumbnail
    {
        get
        {
            if (_thumbnail is null && File.Exists(FullPath))
            {
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.DecodePixelWidth = 240;
                    bmp.UriSource = new Uri(FullPath);
                    bmp.EndInit();
                    bmp.Freeze();
                    _thumbnail = bmp;
                }
                catch
                {
                    // Thumbnail creation failed; return null gracefully
                }
            }
            return _thumbnail;
        }
    }

    private void Notify([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
