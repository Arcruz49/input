using System;

namespace Input.Models;

public sealed class RecordingSession
{
    public long Id { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public string VideoPath { get; set; } = string.Empty;
}