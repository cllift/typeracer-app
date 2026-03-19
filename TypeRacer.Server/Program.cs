using System.Net;
using System.Net.Sockets;
using System.Text;
using TypeRacer.Shared.Enums;
using TypeRacer.Shared.Messages;
using TypeRacer.Shared.Networking;

// Stores room data in memory.
// Key = room code, Value = list of players in that room.
Dictionary<string, List<PlayerInfo>> rooms = new();

Console.WriteLine("Server starting...");

TcpListener server = new TcpListener(IPAddress.Any, 50000);
server.Start();

Console.WriteLine("Server listening on port 50000...");

while (true)
{
    TcpClient client = server.AcceptTcpClient();
    Console.WriteLine("Client connected!");

    try
    {
        using NetworkStream stream = client.GetStream();

        Console.WriteLine("Server waiting to read data...");

        byte[] buffer = new byte[4096];
        int bytesRead = stream.Read(buffer, 0, buffer.Length);

        Console.WriteLine($"Server bytesRead = {bytesRead}");

        if (bytesRead == 0)
        {
            Console.WriteLine("Client disconnected before sending data.");
            client.Close();
            continue;
        }

        string json = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        Console.WriteLine($"Received raw JSON: {json}");

        // First, deserialize the outer envelope.
        MessageEnvelope envelope = MessageSerialiser.Deserialize<MessageEnvelope>(json);

        // Check what type of message the client sent.
        if (envelope.Type == MessageType.JoinRoom)
        {
            // Deserialize the actual JoinRoom message from the payload.
            JoinRoomMessage joinRoomMessage =
                MessageSerialiser.Deserialize<JoinRoomMessage>(envelope.Payload);

            Console.WriteLine(
                $"Player '{joinRoomMessage.PlayerName}' wants to join room '{joinRoomMessage.RoomCode}'.");

            // Create the room if it does not already exist.
            if (!rooms.ContainsKey(joinRoomMessage.RoomCode))
            {
                rooms[joinRoomMessage.RoomCode] = new List<PlayerInfo>();
            }

            // Add the player to the room.
            rooms[joinRoomMessage.RoomCode].Add(new PlayerInfo
            {
                Name = joinRoomMessage.PlayerName
            });

            // Build the current room state to send back.
            RoomStateMessage roomStateMessage = new RoomStateMessage
            {
                RoomCode = joinRoomMessage.RoomCode,
                Players = rooms[joinRoomMessage.RoomCode]
            };

            // Wrap the response in an envelope.
            MessageEnvelope responseEnvelope = new MessageEnvelope
            {
                Type = MessageType.RoomState,
                Payload = MessageSerialiser.Serialize(roomStateMessage)
            };

            // Serialize the response and send it back.
            string responseJson = MessageSerialiser.Serialize(responseEnvelope);
            byte[] responseData = Encoding.UTF8.GetBytes(responseJson);

            stream.Write(responseData, 0, responseData.Length);
            stream.Flush();

            Console.WriteLine($"Sent room state for room '{joinRoomMessage.RoomCode}'.");
        }
        else
        {
            Console.WriteLine($"Unknown message type received: {envelope.Type}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Server error: {ex.Message}");
    }
    finally
    {
        client.Close();
        Console.WriteLine("Connection closed.");
    }
}