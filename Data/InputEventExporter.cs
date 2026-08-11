using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;

namespace Input.Data;

public sealed class InputEventExporter
{
    private readonly string _connectionString;

    public InputEventExporter(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
    }

    public async Task ExportSessionAsync(long sessionId, string videoPath)
    {
        var txtPath = Path.ChangeExtension(videoPath, ".txt");

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT OffsetMs, EventType, KeyOrButton, X, Y
            FROM InputEvents
            WHERE SessionId = $sessionId
            ORDER BY OffsetMs ASC;
            """;
        cmd.Parameters.AddWithValue("$sessionId", sessionId);

        var sb = new StringBuilder();
        sb.AppendLine($"# Sessão {sessionId} — exportado em {DateTime.UtcNow:O}");
        sb.AppendLine();

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var offsetMs = reader.GetInt64(0);
            var eventType = reader.GetString(1);
            var keyOrButton = reader.IsDBNull(2) ? null : reader.GetString(2);
            int? x = reader.IsDBNull(3) ? null : reader.GetInt32(3);
            int? y = reader.IsDBNull(4) ? null : reader.GetInt32(4);

            var ts = TimeSpan.FromMilliseconds(offsetMs);
            sb.Append($"[{ts:mm\\:ss\\.fff}] {eventType,-10}");

            if (keyOrButton is not null)
                sb.Append($" {keyOrButton}");

            if (x is not null && y is not null)
                sb.Append($" ({x},{y})");

            sb.AppendLine();
        }

        await File.WriteAllTextAsync(txtPath, sb.ToString());
    }
}