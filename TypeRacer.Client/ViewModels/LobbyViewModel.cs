using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Threading;
using TypeRacer.Client.Services;
using TypeRacer.Shared.Messages;

namespace TypeRacer.Client.ViewModels;

public class LobbyViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindow;
    private readonly string _playerName;
    private readonly string _roomCode;

    private string _roomCodeDisplay;
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

    public ObservableCollection<PlayerInfo> Players { get; } = new();

    public ICommand ReadyCommand { get; }

    public LobbyViewModel(MainWindowViewModel mainWindow, string playerName, string roomCode)
    {
        _mainWindow = mainWindow;
        _playerName = playerName;
        _roomCode = roomCode;

        _roomCodeDisplay = $"Room Code: {roomCode}";

        Players.Add(new PlayerInfo
        {
            Name = playerName,
            IsReady = false
        });

        ReadyCommand = new RelayCommand(async () => await OnReadyClickedAsync());

        App.NetworkClient.RoomStateReceived += OnRoomStateReceived;
        App.NetworkClient.RaceStartingReceived += OnRaceStartingReceived;
    }

    private void OnRoomStateReceived(RoomStateMessage roomState)
    {
        if (roomState.RoomCode != _roomCode)
            return;

        Dispatcher.UIThread.Post(() =>
        {
            RoomCodeDisplay = $"Room Code: {roomState.RoomCode}";

            Players.Clear();

            foreach (PlayerInfo player in roomState.Players)
            {
                Players.Add(player);
            }

            StatusMessage = $"Players in room: {roomState.Players.Count}";
        });
    }

    private async Task OnReadyClickedAsync()
    {
        StatusMessage = "You are ready. Waiting for other players...";
        await App.NetworkClient.SendReadyAsync(_playerName, _roomCode);
    }

    private void OnRaceStartingReceived(RaceStartingMessage raceStarting)
    {
        if (raceStarting.RoomCode != _roomCode)
            return;

        Dispatcher.UIThread.Post(() =>
        {
            StatusMessage = "Race starting...";
            _mainWindow.ShowRace(_playerName, _roomCode, raceStarting.CountdownSeconds, raceStarting.RaceText);
        });
    }
}