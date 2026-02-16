using FluentAssertions;
using Microsoft.Data.Sqlite;
using VRNotify.Domain.NotificationProcessing;
using VRNotify.Domain.SourceConnection;
using VRNotify.Infrastructure.Persistence;

namespace VRNotify.Infrastructure.Tests.Persistence;

public sealed class SqliteNotificationHistoryTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _dbPath;

    public SqliteNotificationHistoryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "VRNotifyTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _dbPath = Path.Combine(_tempDir, "test_history.db");
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private static NotificationCard CreateCard(
        string title = "Discord",
        string body = "Hello!",
        string sender = "TestUser",
        SourceType sourceType = SourceType.WindowsNotification,
        Priority priority = Priority.Low) => new(
        Guid.NewGuid(),
        Guid.NewGuid(),
        sourceType,
        priority,
        title,
        body,
        sender,
        null,
        TimeSpan.FromSeconds(5));

    [Fact]
    public async Task SaveAndGetRecent_RoundTrips()
    {
        var history = new SqliteNotificationHistory(_dbPath);
        var card = CreateCard();

        await history.SaveAsync(card);
        var recent = await history.GetRecentAsync(10);

        recent.Should().HaveCount(1);
        recent[0].CardId.Should().Be(card.CardId);
        recent[0].Title.Should().Be("Discord");
        recent[0].Body.Should().Be("Hello!");
        recent[0].SenderDisplay.Should().Be("TestUser");
        recent[0].SourceType.Should().Be(SourceType.WindowsNotification);
        recent[0].Priority.Should().Be(Priority.Low);
    }

    [Fact]
    public async Task GetRecent_ReturnsNewestFirst()
    {
        var history = new SqliteNotificationHistory(_dbPath);
        var card1 = CreateCard(title: "App1", body: "First");
        var card2 = CreateCard(title: "App2", body: "Second");
        var card3 = CreateCard(title: "App3", body: "Third");

        await history.SaveAsync(card1);
        await Task.Delay(10); // Ensure different CreatedAt
        await history.SaveAsync(card2);
        await Task.Delay(10);
        await history.SaveAsync(card3);

        var recent = await history.GetRecentAsync(10);

        recent.Should().HaveCount(3);
        recent[0].Body.Should().Be("Third");
        recent[1].Body.Should().Be("Second");
        recent[2].Body.Should().Be("First");
    }

    [Fact]
    public async Task GetRecent_LimitsResults()
    {
        var history = new SqliteNotificationHistory(_dbPath);

        for (int i = 0; i < 5; i++)
        {
            await history.SaveAsync(CreateCard(body: $"Message {i}"));
            await Task.Delay(10);
        }

        var recent = await history.GetRecentAsync(3);

        recent.Should().HaveCount(3);
    }

    [Fact]
    public async Task PurgeOldEntries_RemovesExcessEntries()
    {
        var history = new SqliteNotificationHistory(_dbPath);

        for (int i = 0; i < 10; i++)
        {
            await history.SaveAsync(CreateCard(body: $"Message {i}"));
            await Task.Delay(10);
        }

        await history.PurgeOldEntriesAsync(TimeSpan.FromDays(7), maxCount: 5);
        var remaining = await history.GetRecentAsync(100);

        remaining.Should().HaveCount(5);
    }

    [Fact]
    public async Task SaveAsync_PreservesDisplayedAt()
    {
        var history = new SqliteNotificationHistory(_dbPath);
        var card = CreateCard();
        card.MarkAsDisplayed();

        await history.SaveAsync(card);
        var recent = await history.GetRecentAsync(1);

        recent[0].DisplayedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveAsync_PreservesNullAvatarUrl()
    {
        var history = new SqliteNotificationHistory(_dbPath);
        var card = CreateCard();

        await history.SaveAsync(card);
        var recent = await history.GetRecentAsync(1);

        recent[0].SenderAvatarUrl.Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_UpsertOnDuplicateCardId()
    {
        var history = new SqliteNotificationHistory(_dbPath);
        var card = CreateCard(body: "Original");

        await history.SaveAsync(card);
        card.MarkAsDisplayed();
        await history.SaveAsync(card);

        var recent = await history.GetRecentAsync(10);
        recent.Should().HaveCount(1);
        recent[0].DisplayedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task AutoMigration_CreatesTableOnFirstAccess()
    {
        var history = new SqliteNotificationHistory(_dbPath);

        // First access triggers migration
        var recent = await history.GetRecentAsync(10);

        recent.Should().BeEmpty();
        File.Exists(_dbPath).Should().BeTrue();
    }
}
