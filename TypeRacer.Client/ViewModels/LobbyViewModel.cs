using System.Collections.ObjectModel;
using System.Windows.Input;
using TypeRacer.Client.Services;
using TypeRacer.Shared.Messages;

namespace TypeRacer.Client.ViewModels;

public class LobbyViewModel : ViewModelBase
{
    private string _roomCodeDisplay = string.Empty;
    private string _statusMessage = "Waiting for other players...";

    public string RoomCodeDisplay
    {
        get => _roomCodeDisplay;
        set => SetField(ref _roomCodeDisplay, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    // This holds the current list of players shown in the lobby.
    public ObservableCollection<PlayerInfo> Players { get; } = new();

    public ICommand ReadyCommand { get; }

    public LobbyViewModel(RoomStateMessage roomState)
    {
        // Show the room code at the top of the lobby.
        RoomCodeDisplay = $"Room Code: {roomState.RoomCode}";

        // Add all players returned by the server.
        foreach (PlayerInfo player in roomState.Players)
        {
            Players.Add(player);
        }

        // Hook up the Ready button.
        ReadyCommand = new RelayCommand(OnReadyClicked);
    }

    private void OnReadyClicked()
    {
        // This is temporary for now.
        // Later this will send a PlayerReadyMessage to the server.
        StatusMessage = "You are ready. Waiting for other players...";
    }
}