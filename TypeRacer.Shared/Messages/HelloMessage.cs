namespace TypeRacer.Shared.Messages;

//This message is sent when the client first connects to the server
public class HelloMessage
{
    //This is the name of the player connecting to the server
    public string Name  { get; set; } = string.Empty;
}