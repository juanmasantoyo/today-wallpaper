using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace TodayWallpaper.Core.History;

/// <summary>
/// Persists the wallpaper history as a JSON index at
/// <c>%APPDATA%\TodayWallpaper\history\history.json</c>.
/// Image files live in the same folder, named <c>{timestamp}_{condition}.jpg</c>.
/// Implements a ring buffer: when full, the oldest <em>unpinned</em> entry is evicted.
/// Pinned entries are never auto-deleted.
/// </summary>
public sealed class HistoryStore : IHistoryStore
{
    /// <summary>Default root folder for history files.</summary>
    public static readonly string DefaultHistoryDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TodayWallpaper", "history");

    private readonly string _historyDir;
    private readonly string _indexPath;
    private readonly ILogger<HistoryStore> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Initialises with default APPDATA path.</summary>
    public HistoryStore(ILogger<HistoryStore> logger)
        : this(DefaultHistoryDir, logger)
    {
    }

    /// <summary>Initialises with a custom directory (useful for unit tests).</summary>
    public HistoryStore(string historyDir, ILogger<HistoryStore> logger)
    {
        _historyDir = historyDir;
        _indexPath = Path.Combine(historyDir, "history.json");
        _logger = logger;
    }

    // ── public API ───────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public async Task<IReadOnlyList<HistoryEntry>> GetAllAsync(CancellationToken ct = default)
    {
        var entries = await LoadAsync(ct);
        return entries.AsReadOnly();
    }

    /// <inheritdoc/>
    public async Task AddAsync(HistoryEntry entry, int maxCount, CancellationToken ct = default)
    {
        var entries = await LoadAsync(ct);
        entries.Insert(0, entry);   // newest first

        // Evict oldest unpinned when over limit.
        while (entries.Count > maxCount)
        {
            int victimIdx = entries.FindLastIndex(e => !e.Pinned);
            if (victimIdx < 0) break;  // all pinned — keep them all

            var victim = entries[victimIdx];
            entries.RemoveAt(victimIdx);
            DeleteFile(victim.File);
            _logger.LogDebug("Evicted history entry {Id}", victim.Id);
        }

        await SaveAsync(entries, ct);
        _logger.LogInformation("History entry added: {Id} ({Style})", entry.Id, entry.Style);
    }

    /// <inheritdoc/>
    public async Task SetPinnedAsync(string id, bool pinned, CancellationToken ct = default)
    {
        var entries = await LoadAsync(ct);
        var idx = entries.FindIndex(e => e.Id == id);
        if (idx < 0) return;

        entries[idx] = entries[idx] with { Pinned = pinned };
        await SaveAsync(entries, ct);
        _logger.LogInformation("Entry {Id} pinned={Pinned}", id, pinned);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        var entries = await LoadAsync(ct);
        var idx = entries.FindIndex(e => e.Id == id);
        if (idx < 0) return;

        var entry = entries[idx];
        if (entry.Pinned)
        {
            _logger.LogWarning("Attempted to delete pinned entry {Id} — skipped.", id);
            return;
        }

        entries.RemoveAt(idx);
        DeleteFile(entry.File);
        await SaveAsync(entries, ct);
    }

    // ── private helpers ──────────────────────────────────────────────────────

    private async Task<List<HistoryEntry>> LoadAsync(CancellationToken ct)
    {
        if (!File.Exists(_indexPath)) return [];

        try
        {
            await using var stream = File.OpenRead(_indexPath);
            var list = await JsonSerializer.DeserializeAsync<List<HistoryEntry>>(stream, JsonOpts, ct);
            return list ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load history index; starting fresh.");
            return [];
        }
    }

    private async Task SaveAsync(List<HistoryEntry> entries, CancellationToken ct)
    {
        Directory.CreateDirectory(_historyDir);
        await using var stream = File.Create(_indexPath);
        await JsonSerializer.SerializeAsync(stream, entries, JsonOpts, ct);
    }

    private void DeleteFile(string relativePath)
    {
        var full = Path.IsPathRooted(relativePath)
            ? relativePath
            : Path.Combine(_historyDir, relativePath);

        try { if (File.Exists(full)) File.Delete(full); }
        catch (Exception ex) { _logger.LogWarning(ex, "Could not delete history file {Path}", full); }
    }
}
