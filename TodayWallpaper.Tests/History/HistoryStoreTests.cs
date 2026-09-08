using Microsoft.Extensions.Logging;
using Moq;
using TodayWallpaper.Core.History;
using TodayWallpaper.Core.Weather;
using Xunit;

namespace TodayWallpaper.Tests.History;

public class HistoryStoreTests
{
    private readonly Mock<ILogger<HistoryStore>> _loggerMock = new();

    private static HistoryEntry CreateEntry(string id, bool pinned = false) =>
        new(
            Id: id,
            File: $"{id}.jpg",
            Condition: WeatherCondition.Sunny,
            Temperature: 20,
            Style: "BlurBlobs",
            Pinned: pinned);

    [Fact]
    public async Task GetAllAsync_EmptyStore_ReturnsEmpty()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"history_test_{Guid.NewGuid():N}");
        try
        {
            var store = new HistoryStore(tempDir, _loggerMock.Object);
            var entries = await store.GetAllAsync();
            Assert.Empty(entries);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task GetAllAsync_CorruptedFile_ReturnsEmptyList()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"history_test_{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(tempDir);
            await File.WriteAllTextAsync(Path.Combine(tempDir, "history.json"), "INVALID_JSON_HERE");

            var store = new HistoryStore(tempDir, _loggerMock.Object);
            var entries = await store.GetAllAsync();
            Assert.Empty(entries);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task AddAsync_AddsAndEvictsOldestUnpinned()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"history_test_{Guid.NewGuid():N}");
        try
        {
            var store = new HistoryStore(tempDir, _loggerMock.Object);

            var entry1 = CreateEntry("1");
            var entry2 = CreateEntry("2");
            var entry3 = CreateEntry("3");

            // Max count = 2
            await store.AddAsync(entry1, maxCount: 2);
            await store.AddAsync(entry2, maxCount: 2);

            var list = await store.GetAllAsync();
            Assert.Equal(2, list.Count);
            Assert.Equal("2", list[0].Id);
            Assert.Equal("1", list[1].Id);

            // Adding 3 should evict 1 (the oldest unpinned)
            await store.AddAsync(entry3, maxCount: 2);

            list = await store.GetAllAsync();
            Assert.Equal(2, list.Count);
            Assert.Equal("3", list[0].Id);
            Assert.Equal("2", list[1].Id);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task SetPinnedAsync_PinsEntryAndPreventsEviction()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"history_test_{Guid.NewGuid():N}");
        try
        {
            var store = new HistoryStore(tempDir, _loggerMock.Object);

            var entry1 = CreateEntry("1");
            var entry2 = CreateEntry("2");
            var entry3 = CreateEntry("3");

            await store.AddAsync(entry1, maxCount: 2);
            await store.AddAsync(entry2, maxCount: 2);

            // Pin entry 1
            await store.SetPinnedAsync("1", true);

            // Adding entry 3 should evict entry 2, because 1 is pinned!
            await store.AddAsync(entry3, maxCount: 2);

            var list = await store.GetAllAsync();
            Assert.Equal(2, list.Count);
            Assert.Equal("3", list[0].Id);
            Assert.Equal("1", list[1].Id);
            Assert.True(list[1].Pinned);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task DeleteAsync_DeletesUnpinned_DoesNotDeletePinned()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"history_test_{Guid.NewGuid():N}");
        try
        {
            var store = new HistoryStore(tempDir, _loggerMock.Object);

            var unpinned = CreateEntry("unpinned", pinned: false);
            var pinned = CreateEntry("pinned", pinned: true);

            await store.AddAsync(unpinned, maxCount: 5);
            await store.AddAsync(pinned, maxCount: 5);

            // Delete pinned should do nothing
            await store.DeleteAsync("pinned");
            var list = await store.GetAllAsync();
            Assert.Equal(2, list.Count);

            // Delete unpinned should remove it
            await store.DeleteAsync("unpinned");
            list = await store.GetAllAsync();
            Assert.Single(list);
            Assert.Equal("pinned", list[0].Id);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }
}
