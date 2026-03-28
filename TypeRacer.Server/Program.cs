using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using TypeRacer.Shared.Enums;
using TypeRacer.Shared.Messages;
using TypeRacer.Shared.Networking;

Dictionary<string, List<ClientConnection>> rooms = new();
object roomLock = new();

Console.WriteLine("Server starting...");

TcpListener server = new TcpListener(IPAddress.Any, 50000);
server.Start();

Console.WriteLine("Server listening on port 50000...");

while (true)
{
    TcpClient tcpClient = await server.AcceptTcpClientAsync();
    Console.WriteLine("Client connected!");

    ClientConnection connection = new ClientConnection(tcpClient);
    _ = Task.Run(() => HandleClientAsync(connection));
}

async Task HandleClientAsync(ClientConnection connection)
{
    try
    {
        while (true)
        {
            string? line = await connection.Reader.ReadLineAsync();

            if (line == null)
            {
                Console.WriteLine("Client disconnected.");
                break;
            }

            Console.WriteLine($"SERVER: Received = {line}");

            MessageEnvelope envelope = MessageSerialiser.Deserialize<MessageEnvelope>(line);

            if (envelope.Type == MessageType.JoinRoom)
            {
                JoinRoomMessage joinRoomMessage =
                    MessageSerialiser.Deserialize<JoinRoomMessage>(envelope.Payload);

                connection.PlayerName = joinRoomMessage.PlayerName;
                connection.RoomCode = joinRoomMessage.RoomCode;
                connection.IsReady = false;

                lock (roomLock)
                {
                    if (!rooms.ContainsKey(joinRoomMessage.RoomCode))
                    {
                        rooms[joinRoomMessage.RoomCode] = new List<ClientConnection>();
                    }

                    if (!rooms[joinRoomMessage.RoomCode].Contains(connection))
                    {
                        rooms[joinRoomMessage.RoomCode].Add(connection);
                    }
                }

                await BroadcastRoomStateAsync(joinRoomMessage.RoomCode);
            }
            else if (envelope.Type == MessageType.PlayerReady)
            {
                PlayerReadyMessage readyMessage =
                    MessageSerialiser.Deserialize<PlayerReadyMessage>(envelope.Payload);

                lock (roomLock)
                {
                    if (!string.IsNullOrWhiteSpace(connection.RoomCode) &&
                        connection.RoomCode == readyMessage.RoomCode &&
                        connection.PlayerName == readyMessage.PlayerName)
                    {
                        connection.IsReady = true;
                    }
                }

                await BroadcastRoomStateAsync(readyMessage.RoomCode);

                bool allReady = false;

                lock (roomLock)
                {
                    if (rooms.TryGetValue(readyMessage.RoomCode, out List<ClientConnection>? roomClients) &&
                        roomClients.Count > 0)
                    {
                        allReady = roomClients.All(c => c.IsReady);
                    }
                }

                if (allReady)
                {
                    await BroadcastRaceStartingAsync(readyMessage.RoomCode);
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"SERVER: Client error: {ex.Message}");
    }
    finally
    {
        string? roomCodeToUpdate = null;

        lock (roomLock)
        {
            if (!string.IsNullOrWhiteSpace(connection.RoomCode) &&
                rooms.TryGetValue(connection.RoomCode, out List<ClientConnection>? roomClients))
            {
                roomClients.Remove(connection);
                roomCodeToUpdate = connection.RoomCode;

                if (roomClients.Count == 0)
                {
                    rooms.Remove(connection.RoomCode);
                }
            }
        }

        connection.Dispose();

        if (!string.IsNullOrWhiteSpace(roomCodeToUpdate) && rooms.ContainsKey(roomCodeToUpdate))
        {
            await BroadcastRoomStateAsync(roomCodeToUpdate);
        }
    }
}

async Task BroadcastRoomStateAsync(string roomCode)
{
    List<ClientConnection> roomClients;
    List<PlayerInfo> players;

    lock (roomLock)
    {
        if (!rooms.TryGetValue(roomCode, out List<ClientConnection>? clientsInRoom))
        {
            return;
        }

        roomClients = new List<ClientConnection>(clientsInRoom);

        players = clientsInRoom
            .Where(c => !string.IsNullOrWhiteSpace(c.PlayerName))
            .Select(c => new PlayerInfo
            {
                Name = c.PlayerName!,
                IsReady = c.IsReady
            })
            .ToList();
    }

    RoomStateMessage roomStateMessage = new RoomStateMessage
    {
        RoomCode = roomCode,
        Players = players
    };

    MessageEnvelope envelope = new MessageEnvelope
    {
        Type = MessageType.RoomState,
        Payload = MessageSerialiser.Serialize(roomStateMessage)
    };

    string json = MessageSerialiser.Serialize(envelope);

    foreach (ClientConnection client in roomClients)
    {
        try
        {
            await client.Writer.WriteLineAsync(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SERVER: Failed to send room state: {ex.Message}");
        }
    }
}

async Task BroadcastRaceStartingAsync(string roomCode)
{
    List<ClientConnection> roomClients;

    lock (roomLock)
    {
        if (!rooms.TryGetValue(roomCode, out List<ClientConnection>? clientsInRoom))
        {
            return;
        }

        roomClients = new List<ClientConnection>(clientsInRoom);
    }

    RaceStartingMessage raceStartingMessage = new RaceStartingMessage
    {
        RoomCode = roomCode,
        CountdownSeconds = 3
    };

    MessageEnvelope envelope = new MessageEnvelope
    {
        Type = MessageType.RaceStarting,
        Payload = MessageSerialiser.Serialize(raceStartingMessage)
    };

    string json = MessageSerialiser.Serialize(envelope);

    foreach (ClientConnection client in roomClients)
    {
        try
        {
            await client.Writer.WriteLineAsync(json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SERVER: Failed to send race starting: {ex.Message}");
        }
    }
}

class ClientConnection : IDisposable
{
    public TcpClient TcpClient { get; }
    public StreamReader Reader { get; }
    public StreamWriter Writer { get; }

    public string? PlayerName { get; set; }
    public string? RoomCode { get; set; }
    public bool IsReady { get; set; }

    public ClientConnection(TcpClient tcpClient)
    {
        TcpClient = tcpClient;
        NetworkStream stream = tcpClient.GetStream();
        Reader = new StreamReader(stream);
        Writer = new StreamWriter(stream) { AutoFlush = true };
    }

    public void Dispose()
    {
        TcpClient.Close();
    }
}