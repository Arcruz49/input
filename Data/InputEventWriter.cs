using System.Threading.Channels;
using Microsoft.Data.Sqlite;
using Input.Models;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

namespace Input.Data;

public sealed class InputEventWriter : IAsyncDisposable
{
    private readonly Channel<InputEvent> _channel = Channel.CreateUnbounded<InputEvent>();
    private readonly string _connectionString;
    private readonly Task _writerTask;
    private readonly CancellationTokenSource _cts = new();

    public InputEventWriter(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
        _writerTask = Task.Run(WriteLoopAsync);
    }

    public void Enqueue(InputEvent ev) => _channel.Writer.TryWrite(ev);

    private async Task WriteLoopAsync()
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();

        var buffer = new List<InputEvent>(500);
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(250));

        try
        {
            while (await timer.WaitForNextTickAsync(_cts.Token))
            {
                while (_channel.Reader.TryRead(out var ev))
                    buffer.Add(ev);

                if (buffer.Count > 0)
                {
                    await FlushAsync(conn, buffer);
                    buffer.Clear();
                }
            }
        }
        catch (OperationCanceledException)
        {
            
        }
    }

    private static async Task FlushAsync(SqliteConnection conn, List<InputEvent> events)
    {
        await using var tx = conn.BeginTransaction();
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = """
            INSERT INTO InputEvents (SessionId, OffsetMs, EventType, KeyOrButton, X, Y)
            VALUES ($sessionId, $offset, $type, $key, $x, $y);
            """;

        var pSession = cmd.Parameters.Add("$sessionId", SqliteType.Integer);
        var pOffset  = cmd.Parameters.Add("$offset", SqliteType.Integer);
        var pType    = cmd.Parameters.Add("$type", SqliteType.Text);
        var pKey     = cmd.Parameters.Add("$key", SqliteType.Text);
        var pX       = cmd.Parameters.Add("$x", SqliteType.Integer);
        var pY       = cmd.Parameters.Add("$y", SqliteType.Integer);

        foreach (var ev in events)
        {
            pSession.Value = ev.SessionId;
            pOffset.Value = ev.OffsetMs;
            pType.Value = ev.EventType.ToString();
            pKey.Value = (object?)ev.KeyOrButton ?? DBNull.Value;
            pX.Value = (object?)ev.X ?? DBNull.Value;
            pY.Value = (object?)ev.Y ?? DBNull.Value;
            await cmd.ExecuteNonQueryAsync();
        }
        await tx.CommitAsync();
    }

    private async Task FlushRemainingAsync()
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync();
        var remaining = new List<InputEvent>();
        while (_channel.Reader.TryRead(out var ev))
            remaining.Add(ev);
        if (remaining.Count > 0)
            await FlushAsync(conn, remaining);
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _channel.Writer.Complete();
        try { await _writerTask; } catch (OperationCanceledException) { }
        await FlushRemainingAsync();
    }
}