using System.Text.Json;
using System;
using System.Text.Json.Serialization;
namespace TypeRacer.Shared.Networking;

//This is shared by both client and server so they use the same format
public static class MessageSerialiser
{
    
    //This class converts messages into JSON and then converting JSON back into message objects
    public static string Serialize<T>(T message)
    {
        //converts the object into JSON that can be sent over the network
        return JsonSerializer.Serialize<T>(message);
    }

    //This converts recieved JSON back into the c# object
    public static T Deserialize<T>(string json)
    {
        T? result = JsonSerializer.Deserialize<T>(json);

        if (result == null)
        {
            throw new InvalidOperationException("Failed to deserialize message.");
        }

        return result;
    }

}