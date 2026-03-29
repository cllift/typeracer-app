namespace TypeRacer.Shared.Messages;

public class RaceResultsMessage
{
    public string Type { get; set; } = "race_results";
    public string RoomCode { get; set; } = string.Empty;
    public List<RaceResultEntry> Results { get; set; } = new();
}