using System.Collections.Generic;

namespace TypeRacer.Shared.Messages;

//sent by the server back to the client to describe the current room state
public class RoomStateMessage
{
    //The room code this state belongs to
    public string RoomCode { get; set; } = string.Empty;
    
    //the players currently inside the room
    public List<PlayerInfo> Players { get; set; } = new();
}