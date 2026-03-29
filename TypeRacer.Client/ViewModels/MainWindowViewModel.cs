using System.Collections.Generic;
using TypeRacer.Shared.Messages;

namespace TypeRacer.Client.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private ViewModelBase _currentViewModel = null!;

    public ViewModelBase CurrentViewModel
    {
        get => _currentViewModel;
        set => SetField(ref _currentViewModel, value);
    }

    public MainWindowViewModel()
    {
        CurrentViewModel = new JoinViewModel(this);
    }

    public void ShowLobby(string playerName, string roomCode)
    {
        CurrentViewModel = new LobbyViewModel(this, playerName, roomCode);
    }

    public void ShowRace(string playerName, string roomCode, int countdown, string raceText)
    {
        CurrentViewModel = new RaceViewModel(this, playerName, roomCode, countdown, raceText);
    }
    public void ShowResults(string playerName, string roomCode, List<RaceResultEntry> results)
    {
        CurrentViewModel = new ResultsViewModel(this, playerName, roomCode, results);
    }
}