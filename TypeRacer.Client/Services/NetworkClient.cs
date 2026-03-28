using System;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using TypeRacer.Shared.Enums;
using TypeRacer.Shared.Messages;
using TypeRacer.Shared.Networking;

namespace TypeRacer.Client.Services;

public class NetworkClient
{
    private TcpClient? _client;
    private StreamReader? _reader;
    private StreamWriter? _writer;

    public event Action<RoomStateMessage>? RoomStateReceived;
    public event Action<RaceStartingMessage>? RaceStartingReceived;

    public async Task<bool> ConnectAsync()
    {
        try
        {
            if (_client != null && _client.Connected)
            {
                return true;
            }

            Console.WriteLine("CLIENT: Creating TcpClient...");
            _client = new TcpClient();

            Console.WriteLine("CLIENT: Connecting to iestyn.com:50000...");
            await _client.ConnectAsync("iestyn.com", 50000);

            Console.WriteLine("CLIENT: Connected.");

            NetworkStream stream = _client.GetStream();

            _reader = new StreamReader(stream);
            _writer = new StreamWriter(stream)
            {
                AutoFlush = true
            };

            _ = Task.Run(ListenLoopAsync);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CLIENT: Connection failed: {ex.Message}");
            return false;
        }
    }

    public async Task JoinRoomAsync(string playerName, string roomCode)
    {
        if (_writer == null)
        {
            throw new InvalidOperationException("Client is not connected.");
        }

        JoinRoomMessage joinRoomMessage = new JoinRoomMessage
        {
            PlayerName = playerName,
            RoomCode = roomCode
        };

        MessageEnvelope envelope = new MessageEnvelope
        {
            Type = MessageType.JoinRoom,
            Payload = MessageSerialiser.Serialize(joinRoomMessage)
        };

        string json = MessageSerialiser.Serialize(envelope);
        await _writer.WriteLineAsync(json);
    }

    public async Task SendReadyAsync(string playerName, string roomCode)
    {
        if (_writer == null)
        {
            throw new InvalidOperationException("Client is not connected.");
        }

        PlayerReadyMessage readyMessage = new PlayerReadyMessage
        {
            PlayerName = playerName,
            RoomCode = roomCode
        };

        MessageEnvelope envelope = new MessageEnvelope
        {
            Type = MessageType.PlayerReady,
            Payload = MessageSerialiser.Serialize(readyMessage)
        };

        string json = MessageSerialiser.Serialize(envelope);
        Console.WriteLine($"CLIENT: Sending PlayerReady = {json}");

        await _writer.WriteLineAsync(json);
    }

    private async Task ListenLoopAsync()
    {
        try
        {
            if (_reader == null)
            {
                return;
            }

            while (true)
            {
                string? line = await _reader.ReadLineAsync();

                if (line == null)
                {
                    Console.WriteLine("CLIENT: Server disconnected.");
                    break;
                }

                Console.WriteLine($"CLIENT: Received = {line}");

                MessageEnvelope envelope = MessageSerialiser.Deserialize<MessageEnvelope>(line);

                if (envelope.Type == MessageType.RoomState)
                {
                    RoomStateMessage roomState =
                        MessageSerialiser.Deserialize<RoomStateMessage>(envelope.Payload);

                    RoomStateReceived?.Invoke(roomState);
                }
                else if (envelope.Type == MessageType.RaceStarting)
                {
                    RaceStartingMessage raceStarting =
                        MessageSerialiser.Deserialize<RaceStartingMessage>(envelope.Payload);

                    RaceStartingReceived?.Invoke(raceStarting);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CLIENT: Listen loop error: {ex.Message}");
        }
    }
}