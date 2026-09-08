using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using TodayWallpaper.Core.History;
using TodayWallpaper.Core.Wallpaper;

namespace TodayWallpaper.App.ViewModels;

/// <summary>ViewModel for the wallpaper history window.</summary>
public sealed class HistoryViewModel(
    IHistoryStore historyStore,
    IWallpaperSetter wallpaperSetter) : INotifyPropertyChanged
{
    private ObservableCollection<HistoryEntry> _entries = [];
    private HistoryEntry? _selected;

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>All history entries, newest first.</summary>
    public ObservableCollection<HistoryEntry> Entries
    {
        get => _entries;
        private set { _entries = value; Notify(); }
    }

    /// <summary>Currently selected history entry.</summary>
    public HistoryEntry? Selected
    {
        get => _selected;
        set { _selected = value; Notify(); }
    }

    /// <summary>Loads history entries from the store.</summary>
    public async Task LoadAsync()
    {
        var list = await historyStore.GetAllAsync();
        Entries  = new ObservableCollection<HistoryEntry>(list);
    }

    /// <summary>Applies the selected entry as the current desktop wallpaper.</summary>
    public void ApplySelected()
    {
        if (Selected is null) return;
        var fullPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TodayWallpaper",
            Selected.File);
        wallpaperSetter.Set(fullPath);
    }

    /// <summary>Toggles the pinned state of the selected entry.</summary>
    public async Task TogglePinSelectedAsync()
    {
        if (Selected is null) return;
        await historyStore.SetPinnedAsync(Selected.Id, !Selected.Pinned);
        await LoadAsync();
    }

    /// <summary>Deletes the selected entry (unpinned only).</summary>
    public async Task DeleteSelectedAsync()
    {
        if (Selected is null) return;
        await historyStore.DeleteAsync(Selected.Id);
        await LoadAsync();
    }

    private void Notify([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
