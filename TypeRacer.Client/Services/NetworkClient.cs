using System;
using System.Net.Sockets;
using System.Text;
using TypeRacer.Shared.Enums;
using TypeRacer.Shared.Messages;
using TypeRacer.Shared.Networking;

namespace TypeRacer.Client.Services;

public class NetworkClient
{
    /// <summary>
    /// Connects to the server, sends a JoinRoom request,
    /// then returns the room state response.
    /// </summary>
    public RoomStateMessage? JoinRoom(string playerName, string roomCode)
    {
        try
        {
            Console.WriteLine("CLIENT: Creating TcpClient...");
            using TcpClient client = new TcpClient();

            Console.WriteLine("CLIENT: Connecting to iestyn.com:50000...");
            client.Connect("iestyn.com", 50000);
            Console.WriteLine("CLIENT: Connected.");

            using NetworkStream stream = client.GetStream();
            Console.WriteLine("CLIENT: Stream acquired.");

            // Build the join-room message.
            JoinRoomMessage joinRoomMessage = new JoinRoomMessage
            {
                PlayerName = playerName,
                RoomCode = roomCode
            };

            // Wrap it in an envelope so the server knows the type.
            MessageEnvelope envelope = new MessageEnvelope
            {
                Type = MessageType.JoinRoom,
                Payload = MessageSerialiser.Serialize(joinRoomMessage)
            };

            // Serialize and send.
            string json = MessageSerialiser.Serialize(envelope);
            Console.WriteLine($"CLIENT: JSON = {json}");

            byte[] data = Encoding.UTF8.GetBytes(json);
            Console.WriteLine($"CLIENT: Sending {data.Length} bytes...");

            stream.Write(data, 0, data.Length);
            stream.Flush();

            Console.WriteLine("CLIENT: JoinRoom message sent.");
            Console.WriteLine("CLIENT: Waiting for room state response...");

            // Read server response.
            byte[] buffer = new byte[4096];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);

            Console.WriteLine($"CLIENT: bytesRead = {bytesRead}");

            if (bytesRead == 0)
            {
                Console.WriteLine("CLIENT: Server closed the connection before sending a response.");
                return null;
            }

            string responseJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine($"CLIENT: Received raw JSON: {responseJson}");

            // Deserialize outer envelope.
            MessageEnvelope responseEnvelope =
                MessageSerialiser.Deserialize<MessageEnvelope>(responseJson);

            if (responseEnvelope.Type != MessageType.RoomState)
            {
                Console.WriteLine($"CLIENT: Unexpected response type: {responseEnvelope.Type}");
                return null;
            }

            // Deserialize room state payload.
            RoomStateMessage roomState =
                MessageSerialiser.Deserialize<RoomStateMessage>(responseEnvelope.Payload);

            return roomState;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CLIENT: JoinRoom failed: {ex.Message}");
            return null;
        }
    }
}