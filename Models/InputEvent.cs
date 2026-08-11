namespace Input.Models;

public sealed class InputEvent
{
    public long Id { get; set; }
    public long SessionId { get; set; }

    // Milissegundos desde o início da sessão — não timestamp absoluto.
    // É o que permite sincronizar com o vídeo depois: segundo do vídeo = OffsetMs / 1000.
    public long OffsetMs { get; set; }

    public InputEventType EventType { get; set; }

    // tecla pressionada 
    public string? KeyOrButton { get; set; }

    // posicionamento mouse
    public int? X { get; set; }
    public int? Y { get; set; }
}