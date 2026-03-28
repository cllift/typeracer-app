using TypeRacer.Client.ViewModels;

namespace TypeRacer.Client.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private ViewModelBase? _currentPage;

    public ViewModelBase? CurrentPage
    {
        get => _currentPage;
        set => SetField(ref _currentPage, value);
    }

    public MainWindowViewModel()
    {
        CurrentPage = new JoinViewModel(this);
    }

    public void ShowLobby(string playerName, string roomCode)
    {
        CurrentPage = new LobbyViewModel(this, playerName, roomCode);
    }

    public void ShowRace(int countdown)
    {
        CurrentPage = new RaceViewModel(countdown);
    }
}