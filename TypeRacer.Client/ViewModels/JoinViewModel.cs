using System;
using System.Threading.Tasks;
using System.Windows.Input;
using TypeRacer.Client.Services;

namespace TypeRacer.Client.ViewModels;

public class JoinViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindow;

    private string _playerName = string.Empty;
    private string _roomCode = string.Empty;
    private string _statusMessage = "Enter your details to join a room.";

    public string PlayerName
    {
        get => _playerName;
        set => SetField(ref _playerName, value);
    }

    public string RoomCode
    {
        get => _roomCode;
        set => SetField(ref _roomCode, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    public ICommand JoinRoomCommand { get; }

    public JoinViewModel(MainWindowViewModel mainWindow)
    {
        _mainWindow = mainWindow;
        JoinRoomCommand = new RelayCommand(async () => await JoinRoomAsync());
    }

    private async Task JoinRoomAsync()
    {
        if (string.IsNullOrWhiteSpace(PlayerName))
        {
            StatusMessage = "Please enter a player name.";
            return;
        }

        if (string.IsNullOrWhiteSpace(RoomCode))
        {
            StatusMessage = "Please enter a room code.";
            return;
        }

        bool connected = await App.NetworkClient.ConnectAsync();

        if (!connected)
        {
            StatusMessage = "Failed to connect to the server.";
            return;
        }

        try
        {
            await App.NetworkClient.JoinRoomAsync(PlayerName, RoomCode);
            _mainWindow.ShowLobby(PlayerName, RoomCode);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }
}