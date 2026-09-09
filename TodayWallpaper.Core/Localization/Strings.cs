#pragma warning disable CS1591
using System.Globalization;
using System.Resources;

namespace TodayWallpaper.Core.Localization;

/// <summary>
/// Strongly-typed resource class for localized UI strings.
/// Supports both default (English) and satellite (Spanish) resources.
/// </summary>
public static class Strings
{
    private static readonly ResourceManager ResourceManager =
        new("TodayWallpaper.Core.Localization.Strings", typeof(Strings).Assembly);

    private static CultureInfo? _culture;

    /// <summary>
    /// Overrides the culture used for resource lookups.
    /// </summary>
    public static CultureInfo? Culture
    {
        get => _culture;
        set => _culture = value;
    }

    /// <summary>
    /// Retrieves the localized string for <paramref name="name"/>.
    /// </summary>
    public static string Get(string name)
    {
        try
        {
            var value = ResourceManager.GetString(name, _culture ?? CultureInfo.CurrentUICulture);
            if (!string.IsNullOrEmpty(value)) return value;
        }
        catch
        {
            // Fall back to dictionary
        }

        if (Fallbacks.TryGetValue(name, out var fallback))
            return fallback;

        return name;
    }

    /// <summary>
    /// Formats a localized format string using the active culture.
    /// </summary>
    public static string Format(string pattern, params object?[] args)
    {
        return string.Format(_culture ?? CultureInfo.CurrentUICulture, pattern, args);
    }

    // ── 1. MainWindow.xaml ───────────────────────────────────────────────────
    public static string APP_MAIN_WINDOW_TITLE => Get(nameof(APP_MAIN_WINDOW_TITLE));
    public static string APP_TAB_SCHEDULE => Get(nameof(APP_TAB_SCHEDULE));
    public static string APP_GROUP_SCHEDULE => Get(nameof(APP_GROUP_SCHEDULE));
    public static string APP_CHECK_ENABLE_SCHEDULE => Get(nameof(APP_CHECK_ENABLE_SCHEDULE));
    public static string APP_LABEL_CADENCE => Get(nameof(APP_LABEL_CADENCE));
    public static string APP_RADIO_INTERVAL => Get(nameof(APP_RADIO_INTERVAL));
    public static string APP_TEXT_HOURS_SUFFIX => Get(nameof(APP_TEXT_HOURS_SUFFIX));
    public static string APP_RADIO_DAILY => Get(nameof(APP_RADIO_DAILY));
    public static string APP_HINT_DAILY_FORMAT => Get(nameof(APP_HINT_DAILY_FORMAT));
    public static string APP_CHECK_RUN_ON_STARTUP => Get(nameof(APP_CHECK_RUN_ON_STARTUP));
    public static string APP_LABEL_LAST_GEN_PREFIX => Get(nameof(APP_LABEL_LAST_GEN_PREFIX));
    public static string APP_GROUP_VISUAL_STYLE => Get(nameof(APP_GROUP_VISUAL_STYLE));
    public static string APP_LABEL_GENERATOR_STYLE => Get(nameof(APP_LABEL_GENERATOR_STYLE));
    public static string APP_LABEL_PALETTE_MODE => Get(nameof(APP_LABEL_PALETTE_MODE));
    public static string APP_LABEL_HISTORY_MAX => Get(nameof(APP_LABEL_HISTORY_MAX));
    public static string APP_BTN_SAVE_SCHEDULE => Get(nameof(APP_BTN_SAVE_SCHEDULE));
    public static string APP_BTN_GENERATE_NOW => Get(nameof(APP_BTN_GENERATE_NOW));
    public static string APP_TAB_LOCATION_WEATHER => Get(nameof(APP_TAB_LOCATION_WEATHER));
    public static string APP_GROUP_LOCATION => Get(nameof(APP_GROUP_LOCATION));
    public static string APP_RADIO_AUTO_LOCATION => Get(nameof(APP_RADIO_AUTO_LOCATION));
    public static string APP_LABEL_DETECTED_IP_PREFIX => Get(nameof(APP_LABEL_DETECTED_IP_PREFIX));
    public static string APP_RADIO_MANUAL_LOCATION => Get(nameof(APP_RADIO_MANUAL_LOCATION));
    public static string APP_LABEL_LATITUDE => Get(nameof(APP_LABEL_LATITUDE));
    public static string APP_LABEL_LONGITUDE => Get(nameof(APP_LABEL_LONGITUDE));
    public static string APP_LABEL_CITY => Get(nameof(APP_LABEL_CITY));
    public static string APP_BTN_DETECT_IP => Get(nameof(APP_BTN_DETECT_IP));
    public static string APP_BTN_SAVE_LOCATION => Get(nameof(APP_BTN_SAVE_LOCATION));
    public static string APP_GROUP_WEATHER_DATA => Get(nameof(APP_GROUP_WEATHER_DATA));
    public static string APP_LABEL_WEATHER_LOCATION => Get(nameof(APP_LABEL_WEATHER_LOCATION));
    public static string APP_LABEL_WEATHER_CONDITION => Get(nameof(APP_LABEL_WEATHER_CONDITION));
    public static string APP_LABEL_WEATHER_TEMP => Get(nameof(APP_LABEL_WEATHER_TEMP));
    public static string APP_LABEL_WEATHER_WIND => Get(nameof(APP_LABEL_WEATHER_WIND));
    public static string APP_LABEL_WEATHER_HUMIDITY => Get(nameof(APP_LABEL_WEATHER_HUMIDITY));
    public static string APP_LABEL_WEATHER_CLOUDS => Get(nameof(APP_LABEL_WEATHER_CLOUDS));
    public static string APP_LABEL_WEATHER_PRECIP => Get(nameof(APP_LABEL_WEATHER_PRECIP));
    public static string APP_LABEL_WEATHER_VISIBILITY => Get(nameof(APP_LABEL_WEATHER_VISIBILITY));
    public static string APP_BTN_REFRESH_WEATHER => Get(nameof(APP_BTN_REFRESH_WEATHER));
    public static string APP_TAB_HISTORY => Get(nameof(APP_TAB_HISTORY));
    public static string APP_GROUP_HISTORY_LIST => Get(nameof(APP_GROUP_HISTORY_LIST));
    public static string APP_GROUP_HISTORY_DETAILS => Get(nameof(APP_GROUP_HISTORY_DETAILS));
    public static string APP_HINT_HISTORY_EMPTY => Get(nameof(APP_HINT_HISTORY_EMPTY));
    public static string APP_BTN_HISTORY_APPLY => Get(nameof(APP_BTN_HISTORY_APPLY));
    public static string APP_BTN_HISTORY_PIN => Get(nameof(APP_BTN_HISTORY_PIN));
    public static string APP_BTN_HISTORY_VIEW_FILE => Get(nameof(APP_BTN_HISTORY_VIEW_FILE));
    public static string APP_BTN_HISTORY_DELETE => Get(nameof(APP_BTN_HISTORY_DELETE));

    // ── 2. MainViewModel.cs ──────────────────────────────────────────────────
    public static string VM_STATUS_DEFAULT => Get(nameof(VM_STATUS_DEFAULT));
    public static string VM_LOCATION_DEFAULT => Get(nameof(VM_LOCATION_DEFAULT));
    public static string VM_SCHEDULE_SUMMARY_PAUSED => Get(nameof(VM_SCHEDULE_SUMMARY_PAUSED));
    public static string VM_SCHEDULE_SUMMARY_STARTUP_NOTE => Get(nameof(VM_SCHEDULE_SUMMARY_STARTUP_NOTE));
    public static string VM_SCHEDULE_SUMMARY_INTERVAL_FORMAT => Get(nameof(VM_SCHEDULE_SUMMARY_INTERVAL_FORMAT));
    public static string VM_SCHEDULE_SUMMARY_DAILY_FORMAT => Get(nameof(VM_SCHEDULE_SUMMARY_DAILY_FORMAT));
    public static string VM_LAST_GEN_NEVER => Get(nameof(VM_LAST_GEN_NEVER));
    public static string VM_AGO_DAYS_FORMAT => Get(nameof(VM_AGO_DAYS_FORMAT));
    public static string VM_AGO_HOURS_FORMAT => Get(nameof(VM_AGO_HOURS_FORMAT));
    public static string VM_AGO_MINS_FORMAT => Get(nameof(VM_AGO_MINS_FORMAT));
    public static string VM_WEATHER_NO_DATA => Get(nameof(VM_WEATHER_NO_DATA));
    public static string VM_WEATHER_TEMP_FORMAT => Get(nameof(VM_WEATHER_TEMP_FORMAT));
    public static string VM_STATUS_INIT_LOADING => Get(nameof(VM_STATUS_INIT_LOADING));
    public static string VM_STATUS_INIT_EXPIRED_GEN => Get(nameof(VM_STATUS_INIT_EXPIRED_GEN));
    public static string VM_STATUS_INIT_ERROR => Get(nameof(VM_STATUS_INIT_ERROR));
    public static string VM_STATUS_SAVE_PROGRESS => Get(nameof(VM_STATUS_SAVE_PROGRESS));
    public static string VM_STATUS_AUTOSAVING => Get(nameof(VM_STATUS_AUTOSAVING));
    public static string VM_STATUS_AUTOSAVED => Get(nameof(VM_STATUS_AUTOSAVED));
    public static string VM_STATUS_SAVE_SUCCESS_TASK => Get(nameof(VM_STATUS_SAVE_SUCCESS_TASK));
    public static string VM_STATUS_SAVE_SUCCESS_UNREGISTERED => Get(nameof(VM_STATUS_SAVE_SUCCESS_UNREGISTERED));
    public static string VM_STATUS_SAVE_SUCCESS => Get(nameof(VM_STATUS_SAVE_SUCCESS));
    public static string VM_STATUS_SAVE_ERROR => Get(nameof(VM_STATUS_SAVE_ERROR));
    public static string VM_STATUS_DETECT_IP_PROGRESS => Get(nameof(VM_STATUS_DETECT_IP_PROGRESS));
    public static string VM_STATUS_DETECT_IP_SUCCESS => Get(nameof(VM_STATUS_DETECT_IP_SUCCESS));
    public static string VM_STATUS_DETECT_IP_ERROR => Get(nameof(VM_STATUS_DETECT_IP_ERROR));
    public static string VM_STATUS_WEATHER_PROGRESS => Get(nameof(VM_STATUS_WEATHER_PROGRESS));
    public static string VM_STATUS_WEATHER_SUCCESS => Get(nameof(VM_STATUS_WEATHER_SUCCESS));
    public static string VM_STATUS_WEATHER_ERROR => Get(nameof(VM_STATUS_WEATHER_ERROR));
    public static string VM_STATUS_GEN_PROGRESS => Get(nameof(VM_STATUS_GEN_PROGRESS));
    public static string VM_STATUS_GEN_SUCCESS => Get(nameof(VM_STATUS_GEN_SUCCESS));
    public static string VM_STATUS_GEN_INCOMPLETE => Get(nameof(VM_STATUS_GEN_INCOMPLETE));
    public static string VM_STATUS_GEN_ERROR => Get(nameof(VM_STATUS_GEN_ERROR));
    public static string VM_STATUS_APPLY_NOT_FOUND => Get(nameof(VM_STATUS_APPLY_NOT_FOUND));
    public static string VM_STATUS_APPLY_SUCCESS => Get(nameof(VM_STATUS_APPLY_SUCCESS));
    public static string VM_STATUS_APPLY_ERROR => Get(nameof(VM_STATUS_APPLY_ERROR));
    public static string VM_STATUS_PIN_PINNED => Get(nameof(VM_STATUS_PIN_PINNED));
    public static string VM_STATUS_PIN_UNPINNED => Get(nameof(VM_STATUS_PIN_UNPINNED));
    public static string VM_STATUS_PIN_ERROR => Get(nameof(VM_STATUS_PIN_ERROR));
    public static string VM_STATUS_DELETE_PINNED_BLOCKED => Get(nameof(VM_STATUS_DELETE_PINNED_BLOCKED));
    public static string VM_STATUS_DELETE_SUCCESS => Get(nameof(VM_STATUS_DELETE_SUCCESS));
    public static string VM_STATUS_DELETE_ERROR => Get(nameof(VM_STATUS_DELETE_ERROR));

    // ── 3. TrayIconService.cs ────────────────────────────────────────────────
    public static string TRAY_TOOLTIP => Get(nameof(TRAY_TOOLTIP));
    public static string TRAY_MENU_HEADER => Get(nameof(TRAY_MENU_HEADER));
    public static string TRAY_MENU_OPEN_APP => Get(nameof(TRAY_MENU_OPEN_APP));
    public static string TRAY_MENU_UPDATE_NOW => Get(nameof(TRAY_MENU_UPDATE_NOW));
    public static string TRAY_MENU_PAUSE_RESUME => Get(nameof(TRAY_MENU_PAUSE_RESUME));
    public static string TRAY_MENU_RESTORE_BG => Get(nameof(TRAY_MENU_RESTORE_BG));
    public static string TRAY_MENU_EXIT => Get(nameof(TRAY_MENU_EXIT));
    public static string TRAY_BALLOON_TITLE => Get(nameof(TRAY_BALLOON_TITLE));
    public static string TRAY_BALLOON_GENERATING => Get(nameof(TRAY_BALLOON_GENERATING));
    public static string TRAY_BALLOON_UPDATE_SUCCESS => Get(nameof(TRAY_BALLOON_UPDATE_SUCCESS));
    public static string TRAY_BALLOON_UPDATE_ERROR => Get(nameof(TRAY_BALLOON_UPDATE_ERROR));
    public static string TRAY_STATE_RESUMED => Get(nameof(TRAY_STATE_RESUMED));
    public static string TRAY_STATE_PAUSED => Get(nameof(TRAY_STATE_PAUSED));
    public static string TRAY_BALLOON_STATE_CHANGE => Get(nameof(TRAY_BALLOON_STATE_CHANGE));
    public static string TRAY_DIALOG_RESTORE_TITLE => Get(nameof(TRAY_DIALOG_RESTORE_TITLE));
    public static string TRAY_DIALOG_RESTORE_FILTER => Get(nameof(TRAY_DIALOG_RESTORE_FILTER));
    public static string TRAY_MSGBOX_ERROR_TITLE => Get(nameof(TRAY_MSGBOX_ERROR_TITLE));
    public static string TRAY_MSGBOX_ERROR_MSG => Get(nameof(TRAY_MSGBOX_ERROR_MSG));

    // ── 4. SettingsWindow.xaml ───────────────────────────────────────────────
    public static string SETTINGS_WINDOW_TITLE => Get(nameof(SETTINGS_WINDOW_TITLE));
    public static string SETTINGS_GROUP_HEADER => Get(nameof(SETTINGS_GROUP_HEADER));
    public static string SETTINGS_LABEL_INTERVAL => Get(nameof(SETTINGS_LABEL_INTERVAL));
    public static string SETTINGS_LABEL_PALETTE => Get(nameof(SETTINGS_LABEL_PALETTE));
    public static string SETTINGS_LABEL_GENERATOR => Get(nameof(SETTINGS_LABEL_GENERATOR));
    public static string SETTINGS_LABEL_HISTORY_MAX => Get(nameof(SETTINGS_LABEL_HISTORY_MAX));
    public static string SETTINGS_LABEL_ENABLED => Get(nameof(SETTINGS_LABEL_ENABLED));
    public static string SETTINGS_BTN_SAVE => Get(nameof(SETTINGS_BTN_SAVE));

    // ── 5. Sandbox ───────────────────────────────────────────────────────────
    public static string SANDBOX_WINDOW_TITLE => Get(nameof(SANDBOX_WINDOW_TITLE));
    public static string SANDBOX_GROUP_GENERATOR => Get(nameof(SANDBOX_GROUP_GENERATOR));
    public static string SANDBOX_LABEL_CONDITION => Get(nameof(SANDBOX_LABEL_CONDITION));
    public static string SANDBOX_LABEL_STYLE => Get(nameof(SANDBOX_LABEL_STYLE));
    public static string SANDBOX_LABEL_PALETTE => Get(nameof(SANDBOX_LABEL_PALETTE));
    public static string SANDBOX_GROUP_WEATHER => Get(nameof(SANDBOX_GROUP_WEATHER));
    public static string SANDBOX_LABEL_WMO => Get(nameof(SANDBOX_LABEL_WMO));
    public static string SANDBOX_LABEL_TEMP => Get(nameof(SANDBOX_LABEL_TEMP));
    public static string SANDBOX_LABEL_APPARENT_TEMP => Get(nameof(SANDBOX_LABEL_APPARENT_TEMP));
    public static string SANDBOX_LABEL_WIND => Get(nameof(SANDBOX_LABEL_WIND));
    public static string SANDBOX_LABEL_HUMIDITY => Get(nameof(SANDBOX_LABEL_HUMIDITY));
    public static string SANDBOX_LABEL_PRECIP => Get(nameof(SANDBOX_LABEL_PRECIP));
    public static string SANDBOX_LABEL_CLOUDS => Get(nameof(SANDBOX_LABEL_CLOUDS));
    public static string SANDBOX_LABEL_VISIBILITY => Get(nameof(SANDBOX_LABEL_VISIBILITY));
    public static string SANDBOX_LABEL_DEWPOINT => Get(nameof(SANDBOX_LABEL_DEWPOINT));
    public static string SANDBOX_GROUP_ACTIONS => Get(nameof(SANDBOX_GROUP_ACTIONS));
    public static string SANDBOX_CHECK_AUTO_REFRESH => Get(nameof(SANDBOX_CHECK_AUTO_REFRESH));
    public static string SANDBOX_BTN_RANDOMIZE => Get(nameof(SANDBOX_BTN_RANDOMIZE));
    public static string SANDBOX_BTN_GENERATE => Get(nameof(SANDBOX_BTN_GENERATE));
    public static string SANDBOX_LABEL_SEED_PREFIX => Get(nameof(SANDBOX_LABEL_SEED_PREFIX));
    public static string SANDBOX_OVERLAY_GENERATING => Get(nameof(SANDBOX_OVERLAY_GENERATING));
    public static string SANDBOX_HINT_EMPTY => Get(nameof(SANDBOX_HINT_EMPTY));
    public static string SANDBOX_STATUS_GEN_TIME_PREFIX => Get(nameof(SANDBOX_STATUS_GEN_TIME_PREFIX));
    public static string SANDBOX_STATUS_GEN_TIME_SUFFIX => Get(nameof(SANDBOX_STATUS_GEN_TIME_SUFFIX));
    public static string SANDBOX_BTN_SAVE_AS => Get(nameof(SANDBOX_BTN_SAVE_AS));
    public static string SANDBOX_BTN_APPLY => Get(nameof(SANDBOX_BTN_APPLY));
    public static string SANDBOX_DIALOG_SAVE_TITLE => Get(nameof(SANDBOX_DIALOG_SAVE_TITLE));
    public static string SANDBOX_DIALOG_SAVE_FILTER => Get(nameof(SANDBOX_DIALOG_SAVE_FILTER));

    // ── 6. TaskScheduler ─────────────────────────────────────────────────────
    public static string CORE_TASK_DESCRIPTION => Get(nameof(CORE_TASK_DESCRIPTION));

    // ── 7. Generator Styles ──────────────────────────────────────────────────
    public static string GENERATOR_STYLE_BLUR_BLOBS => Get(nameof(GENERATOR_STYLE_BLUR_BLOBS));
    public static string GENERATOR_STYLE_LOW_POLY => Get(nameof(GENERATOR_STYLE_LOW_POLY));
    public static string GENERATOR_STYLE_RADIAL_GRADIENT => Get(nameof(GENERATOR_STYLE_RADIAL_GRADIENT));
    public static string GENERATOR_STYLE_AURORA_WAVES => Get(nameof(GENERATOR_STYLE_AURORA_WAVES));
    public static string GENERATOR_STYLE_VORONOI_MOSAIC => Get(nameof(GENERATOR_STYLE_VORONOI_MOSAIC));
    public static string GENERATOR_STYLE_ATMOSPHERIC_RIDGES => Get(nameof(GENERATOR_STYLE_ATMOSPHERIC_RIDGES));

    /// <summary>
    /// Resolves the localized display name for a generator style ID.
    /// </summary>
    public static string GetGeneratorStyleName(string styleId) => styleId switch
    {
        "BlurBlobs" => GENERATOR_STYLE_BLUR_BLOBS,
        "LowPoly" => GENERATOR_STYLE_LOW_POLY,
        "RadialGradient" => GENERATOR_STYLE_RADIAL_GRADIENT,
        "AuroraWaves" => GENERATOR_STYLE_AURORA_WAVES,
        "VoronoiMosaic" => GENERATOR_STYLE_VORONOI_MOSAIC,
        "AtmosphericRidges" => GENERATOR_STYLE_ATMOSPHERIC_RIDGES,
        _ => styleId
    };

    // ── 8. Palette Configuration ─────────────────────────────────────────────
    public static string APP_TAB_PALETTES => Get(nameof(APP_TAB_PALETTES));
    public static string APP_GROUP_PALETTE_CONFIG => Get(nameof(APP_GROUP_PALETTE_CONFIG));
    public static string APP_PALETTE_DESCRIPTION => Get(nameof(APP_PALETTE_DESCRIPTION));
    public static string APP_LABEL_EDIT_MODE => Get(nameof(APP_LABEL_EDIT_MODE));
    public static string APP_LABEL_SELECT_CONDITION => Get(nameof(APP_LABEL_SELECT_CONDITION));
    public static string APP_LABEL_PALETTE_SELECT => Get(nameof(APP_LABEL_PALETTE_SELECT));
    public static string APP_LABEL_PALETTE_NAME => Get(nameof(APP_LABEL_PALETTE_NAME));
    public static string APP_CHECK_PALETTE_ENABLED => Get(nameof(APP_CHECK_PALETTE_ENABLED));
    public static string APP_BTN_NEW_PALETTE => Get(nameof(APP_BTN_NEW_PALETTE));
    public static string APP_BTN_DUPLICATE_PALETTE => Get(nameof(APP_BTN_DUPLICATE_PALETTE));
    public static string APP_BTN_DELETE_PALETTE => Get(nameof(APP_BTN_DELETE_PALETTE));
    public static string APP_HINT_DRAG_DROP => Get(nameof(APP_HINT_DRAG_DROP));
    public static string APP_GROUP_PRIMARY_COLORS => Get(nameof(APP_GROUP_PRIMARY_COLORS));
    public static string APP_GROUP_SECONDARY_COLORS => Get(nameof(APP_GROUP_SECONDARY_COLORS));
    public static string APP_GROUP_PALETTE_PREVIEW => Get(nameof(APP_GROUP_PALETTE_PREVIEW));
    public static string APP_BTN_ADD_COLOR => Get(nameof(APP_BTN_ADD_COLOR));
    public static string APP_BTN_REMOVE_COLOR => Get(nameof(APP_BTN_REMOVE_COLOR));
    public static string APP_BTN_PICK_COLOR => Get(nameof(APP_BTN_PICK_COLOR));
    public static string APP_BTN_SAVE_PALETTES => Get(nameof(APP_BTN_SAVE_PALETTES));
    public static string APP_BTN_RESET_CONDITION => Get(nameof(APP_BTN_RESET_CONDITION));
    public static string APP_BTN_RESET_ALL_PALETTES => Get(nameof(APP_BTN_RESET_ALL_PALETTES));
    public static string APP_PALETTE_SAVED_MSG => Get(nameof(APP_PALETTE_SAVED_MSG));
    public static string APP_PALETTE_RESET_MSG => Get(nameof(APP_PALETTE_RESET_MSG));
    public static string APP_PALETTE_INVALID_WARNING => Get(nameof(APP_PALETTE_INVALID_WARNING));
    public static string APP_PALETTE_LUMINANCE_RECOMMENDATION => Get(nameof(APP_PALETTE_LUMINANCE_RECOMMENDATION));
    public static string APP_STATUS_PALETTE_ADDED => Get(nameof(APP_STATUS_PALETTE_ADDED));
    public static string APP_STATUS_PALETTE_DUPLICATED => Get(nameof(APP_STATUS_PALETTE_DUPLICATED));
    public static string APP_STATUS_PALETTE_DELETED => Get(nameof(APP_STATUS_PALETTE_DELETED));
    public static string WEATHER_COND_SUNNY => Get(nameof(WEATHER_COND_SUNNY));
    public static string WEATHER_COND_CLOUDY => Get(nameof(WEATHER_COND_CLOUDY));
    public static string WEATHER_COND_RAINY => Get(nameof(WEATHER_COND_RAINY));
    public static string WEATHER_COND_STORMY => Get(nameof(WEATHER_COND_STORMY));
    public static string WEATHER_COND_SNOWY => Get(nameof(WEATHER_COND_SNOWY));
    public static string WEATHER_COND_FOGGY => Get(nameof(WEATHER_COND_FOGGY));
    public static string PALETTE_MODE_EMPATHIC => Get(nameof(PALETTE_MODE_EMPATHIC));
    public static string PALETTE_MODE_CONTRAST => Get(nameof(PALETTE_MODE_CONTRAST));

    // ── Fallback Dictionary (English) ────────────────────────────────────────
    private static readonly Dictionary<string, string> Fallbacks = new(StringComparer.Ordinal)
    {
        [nameof(APP_MAIN_WINDOW_TITLE)] = "Today Wallpaper",
        [nameof(APP_TAB_SCHEDULE)] = "Schedule",
        [nameof(APP_GROUP_SCHEDULE)] = "Automatic Wallpaper Scheduling",
        [nameof(APP_CHECK_ENABLE_SCHEDULE)] = "Enable automatic wallpaper scheduling",
        [nameof(APP_LABEL_CADENCE)] = "Generation frequency:",
        [nameof(APP_RADIO_INTERVAL)] = "Every",
        [nameof(APP_TEXT_HOURS_SUFFIX)] = "hour(s) (1 - 24 h)",
        [nameof(APP_RADIO_DAILY)] = "Every day at:",
        [nameof(APP_HINT_DAILY_FORMAT)] = "(24h format, e.g. 17:00)",
        [nameof(APP_CHECK_RUN_ON_STARTUP)] = "Check on startup",
        [nameof(APP_LABEL_LAST_GEN_PREFIX)] = "Last generated wallpaper: ",
        [nameof(APP_GROUP_VISUAL_STYLE)] = "Visual Style & Generation",
        [nameof(APP_LABEL_GENERATOR_STYLE)] = "Generator style:",
        [nameof(APP_LABEL_PALETTE_MODE)] = "Palette mode:",
        [nameof(APP_LABEL_HISTORY_MAX)] = "Maximum history wallpapers:",
        [nameof(APP_BTN_SAVE_SCHEDULE)] = "Save schedule",
        [nameof(APP_BTN_GENERATE_NOW)] = "Generate now",
        [nameof(APP_TAB_LOCATION_WEATHER)] = "Location & Weather",
        [nameof(APP_GROUP_LOCATION)] = "User Location Settings",
        [nameof(APP_RADIO_AUTO_LOCATION)] = "Automatic IP detection (Recommended)",
        [nameof(APP_LABEL_DETECTED_IP_PREFIX)] = "Current detected IP location: ",
        [nameof(APP_RADIO_MANUAL_LOCATION)] = "Manual location by geographic coordinates:",
        [nameof(APP_LABEL_LATITUDE)] = "Latitude:",
        [nameof(APP_LABEL_LONGITUDE)] = "Longitude:",
        [nameof(APP_LABEL_CITY)] = "City:",
        [nameof(APP_BTN_DETECT_IP)] = "Detect location",
        [nameof(APP_BTN_SAVE_LOCATION)] = "Save location",
        [nameof(APP_GROUP_WEATHER_DATA)] = "Latest retrieved weather data (Open-Meteo)",
        [nameof(APP_LABEL_WEATHER_LOCATION)] = "Location:",
        [nameof(APP_LABEL_WEATHER_CONDITION)] = "Condition:",
        [nameof(APP_LABEL_WEATHER_TEMP)] = "Temperature:",
        [nameof(APP_LABEL_WEATHER_WIND)] = "Wind:",
        [nameof(APP_LABEL_WEATHER_HUMIDITY)] = "Humidity:",
        [nameof(APP_LABEL_WEATHER_CLOUDS)] = "Cloud cover:",
        [nameof(APP_LABEL_WEATHER_PRECIP)] = "Precipitation:",
        [nameof(APP_LABEL_WEATHER_VISIBILITY)] = "Visibility:",
        [nameof(APP_BTN_REFRESH_WEATHER)] = "Refresh weather",
        [nameof(APP_TAB_HISTORY)] = "Wallpaper history",
        [nameof(APP_GROUP_HISTORY_LIST)] = "Generated Wallpapers",
        [nameof(APP_GROUP_HISTORY_DETAILS)] = "Wallpaper details & preview",
        [nameof(APP_HINT_HISTORY_EMPTY)] = "Select a wallpaper from the history",
        [nameof(APP_BTN_HISTORY_APPLY)] = "Set as wallpaper",
        [nameof(APP_BTN_HISTORY_PIN)] = "Pin / Unpin",
        [nameof(APP_BTN_HISTORY_VIEW_FILE)] = "Open file",
        [nameof(APP_BTN_HISTORY_DELETE)] = "Delete",

        [nameof(VM_STATUS_DEFAULT)] = "Ready.",
        [nameof(VM_LOCATION_DEFAULT)] = "Not checked yet.",
        [nameof(VM_SCHEDULE_SUMMARY_PAUSED)] = "Scheduling disabled (Paused)",
        [nameof(VM_SCHEDULE_SUMMARY_STARTUP_NOTE)] = " · Checks on system startup",
        [nameof(VM_SCHEDULE_SUMMARY_INTERVAL_FORMAT)] = "Active: every {0} h{1}",
        [nameof(VM_SCHEDULE_SUMMARY_DAILY_FORMAT)] = "Active: every day at {0:D2}:{1:D2}{2}",
        [nameof(VM_LAST_GEN_NEVER)] = "Never generated yet",
        [nameof(VM_AGO_DAYS_FORMAT)] = "{0} day(s) ago",
        [nameof(VM_AGO_HOURS_FORMAT)] = "{0} hour(s) ago",
        [nameof(VM_AGO_MINS_FORMAT)] = "{0} min ago",
        [nameof(VM_WEATHER_NO_DATA)] = "No saved weather data",
        [nameof(VM_WEATHER_TEMP_FORMAT)] = "{0:F1} °C (Feels like: {1:F1} °C)",
        [nameof(VM_STATUS_INIT_LOADING)] = "Loading settings and history...",
        [nameof(VM_STATUS_INIT_EXPIRED_GEN)] = "Wallpaper expired. Generating new wallpaper in background...",
        [nameof(VM_STATUS_INIT_ERROR)] = "Initialization error: {0}",
        [nameof(VM_STATUS_SAVE_PROGRESS)] = "Saving settings...",
        [nameof(VM_STATUS_AUTOSAVING)] = "Saving changes...",
        [nameof(VM_STATUS_AUTOSAVED)] = "Changes saved ✓",
        [nameof(VM_STATUS_SAVE_SUCCESS_TASK)] = "Settings saved and scheduled task updated in Windows.",
        [nameof(VM_STATUS_SAVE_SUCCESS_UNREGISTERED)] = "Settings saved and automatic schedule disabled.",
        [nameof(VM_STATUS_SAVE_SUCCESS)] = "Settings saved.",
        [nameof(VM_STATUS_SAVE_ERROR)] = "Error saving settings: {0}",
        [nameof(VM_STATUS_DETECT_IP_PROGRESS)] = "Detecting location by IP...",
        [nameof(VM_STATUS_DETECT_IP_SUCCESS)] = "Location detected: {0}, {1}.",
        [nameof(VM_STATUS_DETECT_IP_ERROR)] = "Error detecting location: {0}",
        [nameof(VM_STATUS_WEATHER_PROGRESS)] = "Querying Open-Meteo weather API...",
        [nameof(VM_STATUS_WEATHER_SUCCESS)] = "Weather data updated: {0}, {1:F1}°C in {2}.",
        [nameof(VM_STATUS_WEATHER_ERROR)] = "Error querying weather: {0}",
        [nameof(VM_STATUS_GEN_PROGRESS)] = "Generating procedural wallpaper and applying...",
        [nameof(VM_STATUS_GEN_SUCCESS)] = "Wallpaper successfully generated and applied!",
        [nameof(VM_STATUS_GEN_INCOMPLETE)] = "Generation did not complete.",
        [nameof(VM_STATUS_GEN_ERROR)] = "Error generating wallpaper: {0}",
        [nameof(VM_STATUS_APPLY_NOT_FOUND)] = "The image file does not exist on disk.",
        [nameof(VM_STATUS_APPLY_SUCCESS)] = "Wallpaper applied: {0}.",
        [nameof(VM_STATUS_APPLY_ERROR)] = "Error applying wallpaper: {0}",
        [nameof(VM_STATUS_PIN_PINNED)] = "Wallpaper pinned in history.",
        [nameof(VM_STATUS_PIN_UNPINNED)] = "Wallpaper unpinned.",
        [nameof(VM_STATUS_PIN_ERROR)] = "Error pinning wallpaper: {0}",
        [nameof(VM_STATUS_DELETE_PINNED_BLOCKED)] = "Cannot delete a pinned wallpaper. Unpin it first.",
        [nameof(VM_STATUS_DELETE_SUCCESS)] = "Wallpaper deleted from history.",
        [nameof(VM_STATUS_DELETE_ERROR)] = "Error deleting wallpaper: {0}",

        [nameof(TRAY_TOOLTIP)] = "Today Wallpaper",
        [nameof(TRAY_MENU_HEADER)] = "Today Wallpaper",
        [nameof(TRAY_MENU_OPEN_APP)] = "Open TodayWallpaper",
        [nameof(TRAY_MENU_UPDATE_NOW)] = "Update wallpaper now",
        [nameof(TRAY_MENU_PAUSE_RESUME)] = "Pause / Resume",
        [nameof(TRAY_MENU_RESTORE_BG)] = "Restore my wallpaper...",
        [nameof(TRAY_MENU_EXIT)] = "Exit",
        [nameof(TRAY_BALLOON_TITLE)] = "Today Wallpaper",
        [nameof(TRAY_BALLOON_GENERATING)] = "Generating new wallpaper...",
        [nameof(TRAY_BALLOON_UPDATE_SUCCESS)] = "Wallpaper updated successfully!",
        [nameof(TRAY_BALLOON_UPDATE_ERROR)] = "Error updating: {0}",
        [nameof(TRAY_STATE_RESUMED)] = "Resumed",
        [nameof(TRAY_STATE_PAUSED)] = "Paused",
        [nameof(TRAY_BALLOON_STATE_CHANGE)] = "Automatic generation {0}.",
        [nameof(TRAY_DIALOG_RESTORE_TITLE)] = "Select a wallpaper image",
        [nameof(TRAY_DIALOG_RESTORE_FILTER)] = "Images|*.jpg;*.jpeg;*.png;*.bmp|All files|*.*",
        [nameof(TRAY_MSGBOX_ERROR_TITLE)] = "Error",
        [nameof(TRAY_MSGBOX_ERROR_MSG)] = "Could not apply wallpaper:\n{0}",

        [nameof(SETTINGS_WINDOW_TITLE)] = "Today Wallpaper — Settings",
        [nameof(SETTINGS_GROUP_HEADER)] = "General Settings",
        [nameof(SETTINGS_LABEL_INTERVAL)] = "Refresh interval (hours):",
        [nameof(SETTINGS_LABEL_PALETTE)] = "Palette mode:",
        [nameof(SETTINGS_LABEL_GENERATOR)] = "Generator style:",
        [nameof(SETTINGS_LABEL_HISTORY_MAX)] = "Max history items:",
        [nameof(SETTINGS_LABEL_ENABLED)] = "Enabled:",
        [nameof(SETTINGS_BTN_SAVE)] = "Save",

        [nameof(SANDBOX_WINDOW_TITLE)] = "TodayWallpaper — Generator Sandbox",
        [nameof(SANDBOX_GROUP_GENERATOR)] = "Generator Configuration",
        [nameof(SANDBOX_LABEL_CONDITION)] = "Condition:",
        [nameof(SANDBOX_LABEL_STYLE)] = "Style:",
        [nameof(SANDBOX_LABEL_PALETTE)] = "Palette:",
        [nameof(SANDBOX_GROUP_WEATHER)] = "Weather Parameters",
        [nameof(SANDBOX_LABEL_WMO)] = "WMO Code (0-99):",
        [nameof(SANDBOX_LABEL_TEMP)] = "Temperature (°C):",
        [nameof(SANDBOX_LABEL_APPARENT_TEMP)] = "Apparent temperature (°C):",
        [nameof(SANDBOX_LABEL_WIND)] = "Wind (km/h):",
        [nameof(SANDBOX_LABEL_HUMIDITY)] = "Humidity (%):",
        [nameof(SANDBOX_LABEL_PRECIP)] = "Precipitation (mm):",
        [nameof(SANDBOX_LABEL_CLOUDS)] = "Cloud cover (%):",
        [nameof(SANDBOX_LABEL_VISIBILITY)] = "Visibility (km):",
        [nameof(SANDBOX_LABEL_DEWPOINT)] = "Dew point (°C):",
        [nameof(SANDBOX_GROUP_ACTIONS)] = "Actions",
        [nameof(SANDBOX_CHECK_AUTO_REFRESH)] = "Auto-refresh on modify",
        [nameof(SANDBOX_BTN_RANDOMIZE)] = "Randomize",
        [nameof(SANDBOX_BTN_GENERATE)] = "Generate",
        [nameof(SANDBOX_LABEL_SEED_PREFIX)] = "Computed seed: ",
        [nameof(SANDBOX_OVERLAY_GENERATING)] = "Generating preview...",
        [nameof(SANDBOX_HINT_EMPTY)] = "Click 'Generate' to create a preview",
        [nameof(SANDBOX_STATUS_GEN_TIME_PREFIX)] = "Generation time: ",
        [nameof(SANDBOX_STATUS_GEN_TIME_SUFFIX)] = " ms",
        [nameof(SANDBOX_BTN_SAVE_AS)] = "Save as...",
        [nameof(SANDBOX_BTN_APPLY)] = "Apply to desktop",
        [nameof(SANDBOX_DIALOG_SAVE_TITLE)] = "Save wallpaper",
        [nameof(SANDBOX_DIALOG_SAVE_FILTER)] = "JPEG|*.jpg|PNG|*.png",

        [nameof(CORE_TASK_DESCRIPTION)] = "Regenerates the desktop wallpaper based on current weather.",

        [nameof(GENERATOR_STYLE_BLUR_BLOBS)] = "Blur Blobs",
        [nameof(GENERATOR_STYLE_LOW_POLY)] = "Low Poly",
        [nameof(GENERATOR_STYLE_RADIAL_GRADIENT)] = "Radial Gradient",
        [nameof(GENERATOR_STYLE_AURORA_WAVES)] = "Aurora Waves",
        [nameof(GENERATOR_STYLE_VORONOI_MOSAIC)] = "Voronoi Mosaic",
        [nameof(GENERATOR_STYLE_ATMOSPHERIC_RIDGES)] = "Atmospheric Ridges",

        [nameof(APP_TAB_PALETTES)] = "Color Palettes",
        [nameof(APP_GROUP_PALETTE_CONFIG)] = "Weather Palette Customization",
        [nameof(APP_PALETTE_DESCRIPTION)] = "Customize primary and secondary colors per weather condition. You can maintain multiple palettes; enabled ones will be chosen randomly during generation.",
        [nameof(APP_LABEL_EDIT_MODE)] = "Palette mode:",
        [nameof(APP_LABEL_SELECT_CONDITION)] = "Weather condition:",
        [nameof(APP_LABEL_PALETTE_SELECT)] = "Palette:",
        [nameof(APP_LABEL_PALETTE_NAME)] = "Palette name:",
        [nameof(APP_CHECK_PALETTE_ENABLED)] = "Enabled for random generation",
        [nameof(APP_BTN_NEW_PALETTE)] = "+ New Palette",
        [nameof(APP_BTN_DUPLICATE_PALETTE)] = "Duplicate",
        [nameof(APP_BTN_DELETE_PALETTE)] = "Delete Palette",
        [nameof(APP_HINT_DRAG_DROP)] = "Drag colors to move between Primary and Secondary or reorder",
        [nameof(APP_GROUP_PRIMARY_COLORS)] = "Primary Colors (Dominant Background Tones)",
        [nameof(APP_GROUP_SECONDARY_COLORS)] = "Secondary Colors (Accent / Highlights)",
        [nameof(APP_GROUP_PALETTE_PREVIEW)] = "Palette Preview",
        [nameof(APP_BTN_ADD_COLOR)] = "+ Add Color",
        [nameof(APP_BTN_REMOVE_COLOR)] = "Remove",
        [nameof(APP_BTN_PICK_COLOR)] = "Pick...",
        [nameof(APP_BTN_SAVE_PALETTES)] = "Save Palettes",
        [nameof(APP_BTN_RESET_CONDITION)] = "Reset Condition",
        [nameof(APP_BTN_RESET_ALL_PALETTES)] = "Reset All Palettes",
        [nameof(APP_PALETTE_SAVED_MSG)] = "Palettes saved successfully.",
        [nameof(APP_PALETTE_RESET_MSG)] = "Palettes reset to built-in defaults.",
        [nameof(APP_PALETTE_INVALID_WARNING)] = "Some colors have invalid hex format.",
        [nameof(APP_PALETTE_LUMINANCE_RECOMMENDATION)] = "Note: Recommended lightness is 20%–55% for optimal icon contrast.",
        [nameof(APP_STATUS_PALETTE_ADDED)] = "New palette created.",
        [nameof(APP_STATUS_PALETTE_DUPLICATED)] = "Palette duplicated.",
        [nameof(APP_STATUS_PALETTE_DELETED)] = "Palette deleted.",
        [nameof(WEATHER_COND_SUNNY)] = "Sunny",
        [nameof(WEATHER_COND_CLOUDY)] = "Cloudy",
        [nameof(WEATHER_COND_RAINY)] = "Rainy",
        [nameof(WEATHER_COND_STORMY)] = "Stormy",
        [nameof(WEATHER_COND_SNOWY)] = "Snowy",
        [nameof(WEATHER_COND_FOGGY)] = "Foggy",
        [nameof(PALETTE_MODE_EMPATHIC)] = "Empathic (Harmonious with weather)",
        [nameof(PALETTE_MODE_CONTRAST)] = "Contrast (Mood-lifting opposites)"
    };
}
