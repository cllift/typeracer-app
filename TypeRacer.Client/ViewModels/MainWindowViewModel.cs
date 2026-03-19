using Avalonia;
using System;
using System.Windows.Input;
using Avalonia.Controls.ApplicationLifetimes;
using TypeRacer.Client.Services;
using TypeRacer.Client.Views;

namespace TypeRacer.Client.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
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

    public MainWindowViewModel()
    {
        JoinRoomCommand = new RelayCommand(JoinRoom);
    }

    private void JoinRoom()
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

        try
        {
            NetworkClient networkClient = new NetworkClient();
            var roomState = networkClient.JoinRoom(PlayerName, RoomCode);

            if (roomState == null)
            {
                StatusMessage = "Failed to join room.";
                return;
            }

            //create lobby room
            LobbyView lobbyView = new LobbyView
            {
                DataContext = new LobbyViewModel(roomState),
            };
            //open the lobby window
            lobbyView.Show();

            //close the join window
            if (Avalonia.Application.Current?.ApplicationLifetime is
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow?.Close();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }
}