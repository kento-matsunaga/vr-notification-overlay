using Microsoft.Data.Sqlite;
using Serilog;
using VRNotify.Domain.NotificationProcessing;
using VRNotify.Domain.SourceConnection;

namespace VRNotify.Infrastructure.Persistence;

public sealed class SqliteNotificationHistory : INotificationHistory
{
    private static readonly string DefaultPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "VRNotify", "history.db");

    private readonly string _connectionString;
    private readonly ILogger _logger;
    private bool _initialized;

    public SqliteNotificationHistory(string? dbPath = null, ILogger? logger = null)
    {
        var path = dbPath ?? DefaultPath;
        _connectionString = $"Data Source={path}";
        _logger = logger ?? Log.ForContext<SqliteNotificationHistory>();
    }

    private async Task EnsureInitializedAsync(CancellationToken ct)
    {
        if (_initialized) return;

        var builder = new SqliteConnectionStringBuilder(_connectionString);
        var directory = Path.GetDirectoryName(builder.DataSource);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS notification_history (
                CardId TEXT PRIMARY KEY,
                OriginEventId TEXT NOT NULL,
                SourceType INTEGER NOT NULL,
                Priority INTEGER NOT NULL,
                State INTEGER NOT NULL,
                Title TEXT NOT NULL,
                Body TEXT NOT NULL,
                SenderDisplay TEXT NOT NULL,
                SenderAvatarUrl TEXT,
                CreatedAt TEXT NOT NULL,
                DisplayedAt TEXT,
                DisplayDurationSeconds REAL NOT NULL
            )
            """;
        await cmd.ExecuteNonQueryAsync(ct);

        cmd.CommandText = """
            CREATE INDEX IF NOT EXISTS idx_notification_history_created
            ON notification_history (CreatedAt DESC)
            """;
        await cmd.ExecuteNonQueryAsync(ct);

        _initialized = true;
        _logger.Debug("Notification history database initialized");
    }

    public async Task SaveAsync(NotificationCard card, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR REPLACE INTO notification_history
            (CardId, OriginEventId, SourceType, Priority, State, Title, Body,
             SenderDisplay, SenderAvatarUrl, CreatedAt, DisplayedAt, DisplayDurationSeconds)
            VALUES
            (@CardId, @OriginEventId, @SourceType, @Priority, @State, @Title, @Body,
             @SenderDisplay, @SenderAvatarUrl, @CreatedAt, @DisplayedAt, @DisplayDurationSeconds)
            """;

        cmd.Parameters.AddWithValue("@CardId", card.CardId.ToString());
        cmd.Parameters.AddWithValue("@OriginEventId", card.OriginEventId.ToString());
        cmd.Parameters.AddWithValue("@SourceType", (int)card.SourceType);
        cmd.Parameters.AddWithValue("@Priority", (int)card.Priority);
        cmd.Parameters.AddWithValue("@State", (int)card.State);
        cmd.Parameters.AddWithValue("@Title", card.Title);
        cmd.Parameters.AddWithValue("@Body", card.Body);
        cmd.Parameters.AddWithValue("@SenderDisplay", card.SenderDisplay);
        cmd.Parameters.AddWithValue("@SenderAvatarUrl", (object?)card.SenderAvatarUrl ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@CreatedAt", card.CreatedAt.ToString("O"));
        cmd.Parameters.AddWithValue("@DisplayedAt",
            card.DisplayedAt.HasValue ? card.DisplayedAt.Value.ToString("O") : DBNull.Value);
        cmd.Parameters.AddWithValue("@DisplayDurationSeconds", card.DisplayDuration.TotalSeconds);

        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<NotificationCard>> GetRecentAsync(int count, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT CardId, OriginEventId, SourceType, Priority, State, Title, Body,
                   SenderDisplay, SenderAvatarUrl, CreatedAt, DisplayedAt, DisplayDurationSeconds
            FROM notification_history
            ORDER BY CreatedAt DESC
            LIMIT @Count
            """;
        cmd.Parameters.AddWithValue("@Count", count);

        var cards = new List<NotificationCard>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            cards.Add(ReadCard(reader));
        }

        return cards;
    }

    public async Task PurgeOldEntriesAsync(TimeSpan maxAge, int maxCount, CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct);

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(ct);

        // Delete entries older than maxAge
        var cutoff = DateTimeOffset.UtcNow - maxAge;
        await using var ageCmd = conn.CreateCommand();
        ageCmd.CommandText = "DELETE FROM notification_history WHERE CreatedAt < @Cutoff";
        ageCmd.Parameters.AddWithValue("@Cutoff", cutoff.ToString("O"));
        var ageDeleted = await ageCmd.ExecuteNonQueryAsync(ct);

        // Delete excess entries beyond maxCount (keep newest)
        await using var countCmd = conn.CreateCommand();
        countCmd.CommandText = """
            DELETE FROM notification_history
            WHERE CardId NOT IN (
                SELECT CardId FROM notification_history
                ORDER BY CreatedAt DESC
                LIMIT @MaxCount
            )
            """;
        countCmd.Parameters.AddWithValue("@MaxCount", maxCount);
        var countDeleted = await countCmd.ExecuteNonQueryAsync(ct);

        if (ageDeleted > 0 || countDeleted > 0)
            _logger.Information("Purged {AgeDeleted} old + {CountDeleted} excess notification history entries",
                ageDeleted, countDeleted);
    }

    private static NotificationCard ReadCard(SqliteDataReader reader)
    {
        return new NotificationCard(
            cardId: Guid.Parse(reader.GetString(0)),
            originEventId: Guid.Parse(reader.GetString(1)),
            sourceType: (SourceType)reader.GetInt32(2),
            priority: (Priority)reader.GetInt32(3),
            state: (NotificationState)reader.GetInt32(4),
            title: reader.GetString(5),
            body: reader.GetString(6),
            senderDisplay: reader.GetString(7),
            senderAvatarUrl: reader.IsDBNull(8) ? null : reader.GetString(8),
            createdAt: DateTimeOffset.Parse(reader.GetString(9)),
            displayedAt: reader.IsDBNull(10) ? null : DateTimeOffset.Parse(reader.GetString(10)),
            readAt: null,
            displayDuration: TimeSpan.FromSeconds(reader.GetDouble(11)));
    }
}
