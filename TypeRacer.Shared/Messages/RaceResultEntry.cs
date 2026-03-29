namespace TypeRacer.Shared.Messages;

public class RaceResultEntry
{
    public string PlayerName { get; set; } = string.Empty;
    public int Position { get; set; }
    public double Wpm { get; set; }
    public bool Finished { get; set; }
}