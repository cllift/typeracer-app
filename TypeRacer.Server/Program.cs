using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using TypeRacer.Shared.Enums;
using TypeRacer.Shared.Messages;
using TypeRacer.Shared.Networking;

string[] availableSentences = LoadSentences();
Random random = new Random();

Dictionary<string, List<ClientConnection>> rooms = new();
Dictionary<string, RoomRaceState> raceStates = new();
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
                        raceStates[joinRoomMessage.RoomCode] = new RoomRaceState();
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
            else if (envelope.Type == MessageType.ProgressUpdate)
            {
                ProgressUpdateMessage progressMessage =
                    MessageSerialiser.Deserialize<ProgressUpdateMessage>(envelope.Payload);

                bool shouldSendResults = false;

                lock (roomLock)
                {
                    if (raceStates.TryGetValue(progressMessage.RoomCode, out RoomRaceState? raceState) &&
                        rooms.TryGetValue(progressMessage.RoomCode, out List<ClientConnection>? roomClients))
                    {
                        raceState.LatestWpm[progressMessage.PlayerName] = progressMessage.Wpm;

                        if (progressMessage.IsFinished &&
                            !raceState.FinishedPlayers.Contains(progressMessage.PlayerName))
                        {
                            raceState.FinishedPlayers.Add(progressMessage.PlayerName);
                            raceState.FinishOrder.Add(progressMessage.PlayerName);
                        }

                        int totalPlayers = roomClients.Count(c => !string.IsNullOrWhiteSpace(c.PlayerName));

                        if (!raceState.ResultsSent &&
                            totalPlayers > 0 &&
                            raceState.FinishedPlayers.Count == totalPlayers)
                        {
                            raceState.ResultsSent = true;
                            shouldSendResults = true;
                        }
                    }
                }

                await BroadcastProgressUpdateAsync(progressMessage);

                if (shouldSendResults)
                {
                    await BroadcastRaceResultsAsync(progressMessage.RoomCode);
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
                    raceStates.Remove(connection.RoomCode);
                }
            }
        }

        connection.Dispose();

        if (!string.IsNullOrWhiteSpace(roomCodeToUpdate) &&
            rooms.ContainsKey(roomCodeToUpdate))
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
    string raceSentence = "The quick brown fox jumps over the lazy dog.";

    lock (roomLock)
    {
        if (!rooms.TryGetValue(roomCode, out List<ClientConnection>? clientsInRoom))
        {
            return;
        }

        roomClients = new List<ClientConnection>(clientsInRoom);

        if (raceStates.TryGetValue(roomCode, out RoomRaceState? raceState))
        {
            raceState.FinishOrder.Clear();
            raceState.FinishedPlayers.Clear();
            raceState.LatestWpm.Clear();
            raceState.ResultsSent = false;
            raceState.CurrentSentence = GetRandomSentence();
            raceSentence = raceState.CurrentSentence;
        }
    }

    RaceStartingMessage raceStartingMessage = new RaceStartingMessage
    {
        RoomCode = roomCode,
        CountdownSeconds = 3,
        RaceText = raceSentence
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

async Task BroadcastProgressUpdateAsync(ProgressUpdateMessage progressMessage)
{
    List<ClientConnection> roomClients;

    lock (roomLock)
    {
        if (!rooms.TryGetValue(progressMessage.RoomCode, out List<ClientConnection>? clientsInRoom))
        {
            return;
        }

        roomClients = new List<ClientConnection>(clientsInRoom);
    }

    MessageEnvelope envelope = new MessageEnvelope
    {
        Type = MessageType.ProgressUpdate,
        Payload = MessageSerialiser.Serialize(progressMessage)
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
            Console.WriteLine($"SERVER: Failed to send progress update: {ex.Message}");
        }
    }
}

async Task BroadcastRaceResultsAsync(string roomCode)
{
    List<ClientConnection> roomClients;
    List<RaceResultEntry> results = new();

    lock (roomLock)
    {
        if (!rooms.TryGetValue(roomCode, out List<ClientConnection>? clientsInRoom))
        {
            return;
        }

        if (!raceStates.TryGetValue(roomCode, out RoomRaceState? raceState))
        {
            return;
        }

        roomClients = new List<ClientConnection>(clientsInRoom);

        for (int i = 0; i < raceState.FinishOrder.Count; i++)
        {
            string playerName = raceState.FinishOrder[i];

            results.Add(new RaceResultEntry
            {
                PlayerName = playerName,
                Position = i + 1,
                Wpm = raceState.LatestWpm.TryGetValue(playerName, out int wpm) ? wpm : 0,
                Finished = true
            });
        }

        var unfinishedPlayers = clientsInRoom
            .Where(c => !string.IsNullOrWhiteSpace(c.PlayerName))
            .Select(c => c.PlayerName!)
            .Where(playerName => !raceState.FinishedPlayers.Contains(playerName));

        foreach (string playerName in unfinishedPlayers)
        {
            results.Add(new RaceResultEntry
            {
                PlayerName = playerName,
                Position = results.Count + 1,
                Wpm = raceState.LatestWpm.TryGetValue(playerName, out int wpm) ? wpm : 0,
                Finished = false
            });
        }
    }

    RaceResultsMessage raceResultsMessage = new RaceResultsMessage
    {
        RoomCode = roomCode,
        Results = results
    };

    MessageEnvelope envelope = new MessageEnvelope
    {
        Type = MessageType.RaceResults,
        Payload = MessageSerialiser.Serialize(raceResultsMessage)
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
            Console.WriteLine($"SERVER: Failed to send race results: {ex.Message}");
        }
    }
}

string[] LoadSentences()
{
    const string filePath = "sentences.txt";

    Console.WriteLine($"SERVER: Looking for sentences file at: {Path.GetFullPath(filePath)}");

    if (!File.Exists(filePath))
    {
        Console.WriteLine("SERVER: sentences.txt not found.");
        return new[]
        {
            "The quick brown fox jumps over the lazy dog."
        };
    }

    string[] lines = File.ReadAllLines(filePath)
        .Where(line => !string.IsNullOrWhiteSpace(line))
        .ToArray();

    if (lines.Length == 0)
    {
        Console.WriteLine("SERVER: sentences.txt is empty.");
        return new[]
        {
            "The quick brown fox jumps over the lazy dog."
        };
    }

    Console.WriteLine($"SERVER: Loaded {lines.Length} sentence(s).");

    foreach (string line in lines)
    {
        Console.WriteLine($"- {line}");
    }

    return lines;
}

string GetRandomSentence()
{
    return availableSentences[random.Next(availableSentences.Length)];
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

class RoomRaceState
{
    public List<string> FinishOrder { get; } = new();
    public HashSet<string> FinishedPlayers { get; } = new();
    public Dictionary<string, int> LatestWpm { get; } = new();
    public bool ResultsSent { get; set; }
    public string CurrentSentence { get; set; } = string.Empty;
}