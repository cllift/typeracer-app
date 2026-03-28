namespace TypeRacer.Shared.Messages;

//represents a player currently in a room
public class PlayerInfo
{
    //the players display name
    public string Name { get; set; } = string.Empty;
    
    //the player being ready to start a race
    public bool IsReady { get; set; }
}