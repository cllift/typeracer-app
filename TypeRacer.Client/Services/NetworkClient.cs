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
    public event Action<ProgressUpdateMessage>? ProgressUpdateReceived;
    public event Action<RaceResultsMessage>? RaceResultsReceived;

    public async Task<bool> ConnectAsync()
    {
        try
        {
            if (_client != null && _client.Connected)
                return true;

            _client = new TcpClient();
            await _client.ConnectAsync("10.101.54.192", 50000);

            NetworkStream stream = _client.GetStream();
            _reader = new StreamReader(stream);
            _writer = new StreamWriter(stream) { AutoFlush = true };

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
            throw new InvalidOperationException("Client is not connected.");

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

        await _writer.WriteLineAsync(MessageSerialiser.Serialize(envelope));
    }

    public async Task SendReadyAsync(string playerName, string roomCode)
    {
        if (_writer == null)
            throw new InvalidOperationException("Client is not connected.");

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

        await _writer.WriteLineAsync(MessageSerialiser.Serialize(envelope));
    }

    public async Task SendProgressAsync(string playerName, string roomCode, double progressPercent, int wpm, bool isFinished)
    {
        if (_writer == null)
            throw new InvalidOperationException("Client is not connected.");

        ProgressUpdateMessage progressMessage = new ProgressUpdateMessage
        {
            PlayerName = playerName,
            RoomCode = roomCode,
            ProgressPercent = progressPercent,
            Wpm = wpm,
            IsFinished = isFinished
        };

        MessageEnvelope envelope = new MessageEnvelope
        {
            Type = MessageType.ProgressUpdate,
            Payload = MessageSerialiser.Serialize(progressMessage)
        };

        await _writer.WriteLineAsync(MessageSerialiser.Serialize(envelope));
    }

    private async Task ListenLoopAsync()
    {
        try
        {
            if (_reader == null)
                return;

            while (true)
            {
                string? line = await _reader.ReadLineAsync();

                if (line == null)
                {
                    Console.WriteLine("CLIENT: Server disconnected.");
                    break;
                }

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
                else if (envelope.Type == MessageType.ProgressUpdate)
                {
                    ProgressUpdateMessage progressUpdate =
                        MessageSerialiser.Deserialize<ProgressUpdateMessage>(envelope.Payload);

                    ProgressUpdateReceived?.Invoke(progressUpdate);
                }
                else if (envelope.Type == MessageType.RaceResults)
                {
                    RaceResultsMessage results =
                        MessageSerialiser.Deserialize<RaceResultsMessage>(envelope.Payload);

                    RaceResultsReceived?.Invoke(results);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CLIENT: Listen loop error: {ex.Message}");
        }
    }
}