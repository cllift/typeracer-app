namespace TypeRacer.Shared.Messages;

//sent by the client when a player wants to join a room
public class JoinRoomMessage
{
    //The player enters their name
    public string PlayerName { get; set; } = string.Empty;
    
    //the room code the player wants to join
    public string RoomCode { get; set; } = string.Empty;
}