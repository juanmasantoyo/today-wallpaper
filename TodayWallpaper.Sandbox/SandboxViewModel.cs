using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using TodayWallpaper.Core;
using TodayWallpaper.Core.Generators;
using TodayWallpaper.Core.Localization;
using TodayWallpaper.Core.Palette;
using TodayWallpaper.Core.Wallpaper;
using TodayWallpaper.Core.Weather;

namespace TodayWallpaper.Sandbox;

/// <summary>ViewModel for the Sandbox calibration window.</summary>
public sealed class SandboxViewModel : INotifyPropertyChanged
{
    private readonly WallpaperGeneratorFactory _factory;
    private readonly IPaletteMapper _mapper;
    private readonly IWallpaperSetter _wallpaperSetter;
    private readonly IScreenResolutionProvider _screenResolution;
    private readonly ILogger<SandboxViewModel> _logger;

    private bool _refreshPending;
    private string? _lastTempPath;

    // ── backing fields ───────────────────────────────────────────────────────
    private WeatherCondition _condition = WeatherCondition.Sunny;
    private string           _styleId   = "BlurBlobs";
    private PaletteMode      _mode      = PaletteMode.Empathic;
    private double _temperature    = 20;
    private double _apparentTemp   = 20;
    private double _windspeed      = 15;
    private double _humidity       = 50;
    private double _precipitation  = 0;
    private double _cloudCover     = 30;
    private double _visibility     = 20;
    private double _dewpoint       = 8;
    private int    _wmoCode        = 0;
    private bool   _autoRefresh    = false;
    private int    _seed;
    private long   _generationMs;
    private BitmapImage? _preview;
    private bool   _isGenerating;

    // ── constructor ──────────────────────────────────────────────────────────

    /// <summary>Initialises with services resolved from DI.</summary>
    public SandboxViewModel(
        WallpaperGeneratorFactory factory,
        IPaletteMapper mapper,
        IWallpaperSetter wallpaperSetter,
        IScreenResolutionProvider screenResolution,
        ILogger<SandboxViewModel> logger)
    {
        _factory          = factory;
        _mapper           = mapper;
        _wallpaperSetter  = wallpaperSetter;
        _screenResolution = screenResolution;
        _logger           = logger;
    }

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    // ── enum collections ─────────────────────────────────────────────────────

    /// <summary>All available weather conditions.</summary>
    public ObservableCollection<WeatherCondition> Conditions { get; }
        = new(Enum.GetValues<WeatherCondition>());

    /// <summary>All available palette modes.</summary>
    public ObservableCollection<PaletteMode> Modes { get; }
        = new(Enum.GetValues<PaletteMode>());

    /// <summary>All registered generator style options with localized names.</summary>
    public ObservableCollection<GeneratorStyleOption> Styles { get; } = [];

    // ── bound properties ─────────────────────────────────────────────────────

    /// <summary>Selected weather condition bucket.</summary>
    public WeatherCondition Condition { get => _condition; set { _condition = value; Notify(); TriggerAutoRefresh(); } }
    /// <summary>Selected generator style.</summary>
    public string           StyleId   { get => _styleId;   set { _styleId   = value; Notify(); TriggerAutoRefresh(); } }
    /// <summary>Selected palette mode.</summary>
    public PaletteMode      Mode      { get => _mode;      set { _mode      = value; Notify(); TriggerAutoRefresh(); } }

    /// <summary>Air temperature (°C).</summary>
    public double Temperature   { get => _temperature;   set { _temperature   = value; Notify(); TriggerAutoRefresh(); } }
    /// <summary>Apparent temperature (°C).</summary>
    public double ApparentTemp  { get => _apparentTemp;  set { _apparentTemp  = value; Notify(); TriggerAutoRefresh(); } }
    /// <summary>Wind speed (km/h).</summary>
    public double Windspeed     { get => _windspeed;     set { _windspeed     = value; Notify(); TriggerAutoRefresh(); } }
    /// <summary>Relative humidity (%).</summary>
    public double Humidity      { get => _humidity;      set { _humidity      = value; Notify(); TriggerAutoRefresh(); } }
    /// <summary>Precipitation (mm).</summary>
    public double Precipitation { get => _precipitation; set { _precipitation = value; Notify(); TriggerAutoRefresh(); } }
    /// <summary>Cloud cover (%).</summary>
    public double CloudCover    { get => _cloudCover;    set { _cloudCover    = value; Notify(); TriggerAutoRefresh(); } }
    /// <summary>Visibility (km).</summary>
    public double Visibility    { get => _visibility;    set { _visibility    = value; Notify(); TriggerAutoRefresh(); } }
    /// <summary>Dew-point temperature (°C).</summary>
    public double Dewpoint      { get => _dewpoint;      set { _dewpoint      = value; Notify(); TriggerAutoRefresh(); } }
    /// <summary>Exact WMO code (0-99).</summary>
    public int    WmoCode       { get => _wmoCode;       set { _wmoCode       = Math.Clamp(value, 0, 99); Notify(); TriggerAutoRefresh(); } }

    /// <summary>When enabled, re-generates on every slider change immediately.</summary>
    public bool AutoRefresh
    {
        get => _autoRefresh;
        set
        {
            _autoRefresh = value;
            Notify();
            if (value)
            {
                TriggerAutoRefresh();
            }
            else
            {
                _refreshPending = false;
            }
        }
    }

    /// <summary>Computed seed for the current weather snapshot.</summary>
    public int Seed { get => _seed; private set { _seed = value; Notify(); } }
    /// <summary>Last generation time in milliseconds.</summary>
    public long GenerationMs { get => _generationMs; private set { _generationMs = value; Notify(); } }
    /// <summary>Preview image (1280×720).</summary>
    public BitmapImage? Preview { get => _preview; private set { _preview = value; Notify(); } }
    /// <summary>True while a generation is in progress.</summary>
    public bool IsGenerating { get => _isGenerating; private set { _isGenerating = value; Notify(); } }

    // ── commands ─────────────────────────────────────────────────────────────

    /// <summary>Initialises the style list from registered generators.</summary>
    public void Initialize()
    {
        Styles.Clear();
        foreach (var id in _factory.StyleIds)
            Styles.Add(new GeneratorStyleOption(id, Strings.GetGeneratorStyleName(id)));
        if (Styles.Count > 0) StyleId = Styles[0].StyleId;
    }

    /// <summary>Fills all sliders with preset values for the current condition.</summary>
    public void Randomize()
    {
        var preset = WeatherPreset.For(Condition);
        _temperature   = preset.TemperatureCelsius;
        _apparentTemp  = preset.ApparentTemperatureCelsius;
        _windspeed     = preset.WindspeedKmh;
        _humidity      = preset.Humidity;
        _precipitation = preset.PrecipitationMm;
        _cloudCover    = preset.CloudCoverPercent;
        _visibility    = preset.VisibilityKm;
        _dewpoint      = preset.DewpointCelsius;
        _wmoCode       = preset.WmoCode;

        Notify(nameof(Temperature));
        Notify(nameof(ApparentTemp));
        Notify(nameof(Windspeed));
        Notify(nameof(Humidity));
        Notify(nameof(Precipitation));
        Notify(nameof(CloudCover));
        Notify(nameof(Visibility));
        Notify(nameof(Dewpoint));
        Notify(nameof(WmoCode));

        TriggerAutoRefresh();
    }

    /// <summary>Generates the preview image immediately.</summary>
    public async Task GenerateAsync()
    {
        if (IsGenerating)
        {
            _refreshPending = true;
            return;
        }

        IsGenerating = true;

        try
        {
            do
            {
                _refreshPending = false;

                var weather = BuildWeatherData();
                Seed = WeatherSeed.Build(weather);

                var palette   = _mapper.Map(weather.Condition, Mode, weather);
                var generator = _factory.Resolve(StyleId, weather);

                var tempPath = Path.Combine(Path.GetTempPath(), $"sandbox_{Guid.NewGuid():N}.png");
                int width = Math.Max(1920, _screenResolution.GetWidth());
                int height = Math.Max(1080, _screenResolution.GetHeight());

                var sw = Stopwatch.StartNew();
                await Task.Run(async () => await generator.GenerateAsync(palette, weather, width, height, tempPath));
                sw.Stop();

                GenerationMs = sw.ElapsedMilliseconds;
                Preview      = LoadBitmap(tempPath);

                TryDeleteFile(_lastTempPath);
                _lastTempPath = tempPath;

                _logger.LogDebug("Sandbox generated in {Ms} ms (seed={Seed})", GenerationMs, Seed);
            }
            while (_refreshPending);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sandbox generation failed.");
        }
        finally
        {
            _refreshPending = false;
            IsGenerating = false;
        }
    }

    /// <summary>Saves the last preview to a user-chosen file.</summary>
    public async Task SaveAsAsync()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title            = TodayWallpaper.Core.Localization.Strings.SANDBOX_DIALOG_SAVE_TITLE,
            Filter           = TodayWallpaper.Core.Localization.Strings.SANDBOX_DIALOG_SAVE_FILTER,
            FileName         = $"wallpaper_{Condition}_{StyleId}.png",
            DefaultExt       = ".png"
        };
        if (dialog.ShowDialog() != true) return;

        var weather   = BuildWeatherData();
        var palette   = _mapper.Map(weather.Condition, Mode, weather);
        var generator = _factory.Resolve(StyleId, weather);
        int width     = _screenResolution.GetWidth();
        int height    = _screenResolution.GetHeight();
        await generator.GenerateAsync(palette, weather, width, height, dialog.FileName);
    }

    /// <summary>Applies the current preview directly as the desktop wallpaper.</summary>
    public async Task ApplyToDesktopAsync()
    {
        var weather   = BuildWeatherData();
        var palette   = _mapper.Map(weather.Condition, Mode, weather);
        var generator = _factory.Resolve(StyleId, weather);
        var path      = Core.Wallpaper.WallpaperSetter.CurrentWallpaperPath;
        int width     = _screenResolution.GetWidth();
        int height    = _screenResolution.GetHeight();
        await generator.GenerateAsync(palette, weather, width, height, path);
        _wallpaperSetter.Set(path);
    }

    // ── helpers ──────────────────────────────────────────────────────────────

    private WeatherData BuildWeatherData() => new(
        Condition:                  Condition,
        WmoCode:                    WmoCode,
        TemperatureCelsius:         Temperature,
        ApparentTemperatureCelsius: ApparentTemp,
        WindspeedKmh:               Windspeed,
        Humidity:                   Humidity,
        PrecipitationMm:            Precipitation,
        CloudCoverPercent:          CloudCover,
        VisibilityKm:               Visibility,
        DewpointCelsius:            Dewpoint,
        LocationName:               "Sandbox");

    private void TriggerAutoRefresh()
    {
        if (!AutoRefresh) return;
        _ = GenerateAsync();
    }

    private static BitmapImage LoadBitmap(string path)
    {
        var bmp = new BitmapImage();
        bmp.BeginInit();
        bmp.CacheOption = BitmapCacheOption.OnLoad;
        using (var stream = File.OpenRead(path))
        {
            bmp.StreamSource = stream;
            bmp.EndInit();
        }
        bmp.Freeze();
        return bmp;
    }

    private static void TryDeleteFile(string? path)
    {
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // Ignore if in use or locked
        }
    }

    private void Notify([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
