namespace TypeRacer.Shared.Messages;

public class RaceStartingMessage
{
    public string RoomCode { get; set; } = string.Empty;
    public int CountdownSeconds { get; set; }
    public string RaceText { get; set; } = string.Empty;
}