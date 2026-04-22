namespace TypeRacer.Shared.Messages;

/// <summary>
/// Sent by the client when the player presses the Ready button.
/// </summary>
public class PlayerReadyMessage
{
    /// The room the player belongs to.
    public string RoomCode { get; set; } = string.Empty;
    
    /// The player's name.
    public string PlayerName { get; set; } = string.Empty;
}