using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using TypeRacer.Client.Services;
using TypeRacer.Shared.Messages;

namespace TypeRacer.Client.ViewModels;

public class ResultsViewModel : ViewModelBase
{
    private readonly MainWindowViewModel _mainWindow;
    private readonly string _playerName;
    private readonly string _roomCode;

    public ObservableCollection<ResultRowViewModel> Results { get; } = new();

    private string _winnerText = string.Empty;
    public string WinnerText
    {
        get => _winnerText;
        set => SetField(ref _winnerText, value);
    }

    public ICommand PlayAgainCommand { get; }

    public ResultsViewModel(
        MainWindowViewModel mainWindow,
        string playerName,
        string roomCode,
        IEnumerable<RaceResultEntry> results)
    {
        _mainWindow = mainWindow;
        _playerName = playerName;
        _roomCode = roomCode;

        PlayAgainCommand = new RelayCommand(OnPlayAgainClicked);

        var orderedResults = results
            .OrderBy(r => r.Position)
            .ToList();

        foreach (var result in orderedResults)
        {
            Results.Add(new ResultRowViewModel
            {
                PositionText = result.Position.ToString(),
                PlayerName = result.PlayerName,
                WpmText = $"{result.Wpm:F0} WPM",
                StatusText = result.Finished ? "Finished" : "DNF"
            });
        }

        var winner = orderedResults.FirstOrDefault(r => r.Finished);

        WinnerText = winner != null
            ? $"Winner: {winner.PlayerName} ({winner.Wpm:F0} WPM)"
            : "Race complete";
    }

    private void OnPlayAgainClicked()
    {
        _mainWindow.ShowLobby(_playerName, _roomCode);
    }
}

public class ResultRowViewModel
{
    public string PositionText { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public string WpmText { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
}