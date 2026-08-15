namespace Input.Models;

public sealed class InputEvent
{
    public long Id { get; set; }
    public long SessionId { get; set; }

    public long OffsetMs { get; set; }

    public InputEventType EventType { get; set; }

    public string? KeyOrButton { get; set; }

    public int? X { get; set; }
    public int? Y { get; set; }
}