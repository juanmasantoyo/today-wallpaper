namespace TodayWallpaper.Core.History;

/// <summary>Manages the rolling wallpaper history with pinning support.</summary>
public interface IHistoryStore
{
    /// <summary>Returns all history entries, newest first.</summary>
    Task<IReadOnlyList<HistoryEntry>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Appends a new entry and evicts the oldest unpinned entry when the ring buffer is full.
    /// </summary>
    Task AddAsync(HistoryEntry entry, int maxCount, CancellationToken ct = default);

    /// <summary>Toggles the pinned state of the entry identified by <paramref name="id"/>.</summary>
    Task SetPinnedAsync(string id, bool pinned, CancellationToken ct = default);

    /// <summary>Deletes the entry and its image file (only if not pinned).</summary>
    Task DeleteAsync(string id, CancellationToken ct = default);
}
