namespace TypeRacer.Shared.Messages;

/// <summary>
/// Sent by the server when all players in the room are ready.
/// </summary>
public class RaceStartingMessage
{

    // The room where the race is about to start.

    public string RoomCode { get; set; } = string.Empty;


    //The number of seconds to count down before the race starts.

    public int CountdownSeconds { get; set; }
}
