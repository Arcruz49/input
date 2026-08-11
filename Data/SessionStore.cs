using Microsoft.Data.Sqlite;
using Input.Models;
using System;

namespace Input.Data;

public sealed class SessionStore
{
    private readonly string _connectionString;

    public SessionStore(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
        EnsureSchema();
    }

    private void EnsureSchema()
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS Sessions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                StartedAtUtc TEXT NOT NULL,
                EndedAtUtc TEXT,
                VideoPath TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS InputEvents (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                SessionId INTEGER NOT NULL,
                OffsetMs INTEGER NOT NULL,
                EventType TEXT NOT NULL,
                KeyOrButton TEXT,
                X INTEGER,
                Y INTEGER,
                FOREIGN KEY (SessionId) REFERENCES Sessions(Id)
            );
            CREATE INDEX IF NOT EXISTS IX_InputEvents_Session ON InputEvents(SessionId);
            """;
        cmd.ExecuteNonQuery();
    }

    public long CreateSession(string videoPath)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Sessions (StartedAtUtc, VideoPath) VALUES ($startedAt, $videoPath);
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("$startedAt", DateTime.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("$videoPath", videoPath);
        return (long)cmd.ExecuteScalar()!;
    }

    public void CloseSession(long sessionId)
    {
        using var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Sessions SET EndedAtUtc = $endedAt WHERE Id = $id;";
        cmd.Parameters.AddWithValue("$endedAt", DateTime.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("$id", sessionId);
        cmd.ExecuteNonQuery();
    }
}