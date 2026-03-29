namespace TypeRacer.Shared.Messages;

public class ProgressUpdateMessage
{
    public string RoomCode { get; set; }
    public string PlayerName { get; set; }
    public double ProgressPercent { get; set; }
    public int Wpm { get; set; }
    public bool IsFinished { get; set; }
}