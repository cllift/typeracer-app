using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using TypeRacer.Client.Models;
using TypeRacer.Shared.Messages;

namespace TypeRacer.Client.ViewModels;

public class RaceViewModel : ViewModelBase
{
    private readonly string _playerName;
    private readonly string _roomCode;
    private readonly MainWindowViewModel _mainWindowViewModel;

    private string _countdownText = string.Empty;
    private string _statusMessage = "Get ready...";
    private string _raceText = "";
    private string _typedText = string.Empty;
    private string _wpmText = "WPM: 0";

    private bool _isCountdownVisible = true;
    private bool _isRaceVisible = false;
    private DateTime? _raceStartTime;
    private bool _hasSentFinishedMessage;

    public string CountdownText
    {
        get => _countdownText;
        set => SetField(ref _countdownText, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    public string RaceText
    {
        get => _raceText;
        set
        {
            if (SetField(ref _raceText, value))
            {
                UpdateDisplayedText();
            }
        }
    }

    public string TypedText
    {
        get => _typedText;
        set
        {
            if (SetField(ref _typedText, value))
            {
                UpdateDisplayedText();
                UpdateTypingState();
            }
        }
    }

    public string WpmText
    {
        get => _wpmText;
        set => SetField(ref _wpmText, value);
    }

    public bool IsCountdownVisible
    {
        get => _isCountdownVisible;
        set => SetField(ref _isCountdownVisible, value);
    }

    public bool IsRaceVisible
    {
        get => _isRaceVisible;
        set => SetField(ref _isRaceVisible, value);
    }

    public ObservableCollection<RaceCharacterInfo> DisplayCharacters { get; } = new();
    public ObservableCollection<OpponentProgressInfo> Opponents { get; } = new();

    public RaceViewModel(MainWindowViewModel mainWindowViewModel, string playerName, string roomCode, int countdownSeconds, string raceText)
    {
        _mainWindowViewModel = mainWindowViewModel;
        _playerName = playerName;
        _roomCode = roomCode;
        _raceText = raceText;

        UpdateDisplayedText();

        App.NetworkClient.ProgressUpdateReceived += OnProgressUpdateReceived;
        App.NetworkClient.RaceResultsReceived += OnRaceResultsReceived;

        StartCountdown(countdownSeconds);
    }
    private async void StartCountdown(int countdownSeconds)
    {
        for (int i = countdownSeconds; i > 0; i--)
        {
            CountdownText = i.ToString();
            await Task.Delay(1000);
        }

        CountdownText = "GO!";
        await Task.Delay(700);

        IsCountdownVisible = false;
        IsRaceVisible = true;
        StatusMessage = "Type the text as fast as you can.";
        _raceStartTime = DateTime.UtcNow;
    }

    private void UpdateDisplayedText()
    {
        DisplayCharacters.Clear();

        for (int i = 0; i < RaceText.Length; i++)
        {
            char raceChar = RaceText[i];

            RaceCharacterInfo info = new RaceCharacterInfo
            {
                Character = raceChar == ' ' ? "\u00A0" : raceChar.ToString(),
                ForegroundColor = "#6E6E6E",
                BackgroundColor = "Transparent"
            };

            if (i < TypedText.Length)
            {
                if (TypedText[i] == raceChar)
                {
                    info.ForegroundColor = "#F2F2F2";
                }
                else
                {
                    info.ForegroundColor = "#FF4D4D";
                }
            }
            else if (i == TypedText.Length)
            {
                info.ForegroundColor = "#F2F2F2";
                info.BackgroundColor = "#D4A017";
            }

            DisplayCharacters.Add(info);
        }
    }

    private async void UpdateTypingState()
    {
        if (!IsRaceVisible || _raceStartTime == null)
            return;

        if (TypedText.Length > RaceText.Length)
        {
            TypedText = TypedText[..RaceText.Length];
            return;
        }

        int correctCharacters = 0;

        for (int i = 0; i < TypedText.Length; i++)
        {
            if (i < RaceText.Length && TypedText[i] == RaceText[i])
                correctCharacters++;
        }

        double minutesElapsed = (DateTime.UtcNow - _raceStartTime.Value).TotalMinutes;
        int wpm = 0;

        if (minutesElapsed > 0)
        {
            int wordsTyped = correctCharacters / 5;
            wpm = (int)Math.Round(wordsTyped / minutesElapsed);
            WpmText = $"WPM: {wpm}";
        }

        if (TypedText == RaceText)
        {
            StatusMessage = "Finished!";
        }
        else if (TypedText.Length > 0)
        {
            int lastIndex = TypedText.Length - 1;
            StatusMessage = (lastIndex < RaceText.Length && TypedText[lastIndex] == RaceText[lastIndex])
                ? "Correct"
                : "Incorrect";
        }

        double progressPercent = RaceText.Length == 0
            ? 0
            : (double)TypedText.Length / RaceText.Length * 100.0;

        bool isFinished = TypedText == RaceText;

        if (isFinished && _hasSentFinishedMessage)
            return;

        if (isFinished)
            _hasSentFinishedMessage = true;

        await App.NetworkClient.SendProgressAsync(
            _playerName,
            _roomCode,
            progressPercent,
            wpm,
            isFinished);
    }

    private void OnProgressUpdateReceived(ProgressUpdateMessage progressUpdate)
    {
        if (progressUpdate.PlayerName == _playerName)
            return;

        Dispatcher.UIThread.Post(() =>
        {
            var existing = Opponents.FirstOrDefault(o => o.Name == progressUpdate.PlayerName);

            if (existing == null)
            {
                existing = new OpponentProgressInfo
                {
                    Name = progressUpdate.PlayerName
                };

                Opponents.Add(existing);
            }

            existing.ProgressPercent = progressUpdate.ProgressPercent;
            existing.WpmText = $"WPM: {progressUpdate.Wpm}";
            existing.FinishedText = progressUpdate.IsFinished ? "Finished!" : string.Empty;
        });
    }

    private void OnRaceResultsReceived(RaceResultsMessage message)
    {
        if (message.RoomCode != _roomCode)
            return;

        Dispatcher.UIThread.Post(() =>
        {
            _mainWindowViewModel.ShowResults(_playerName, _roomCode, message.Results);
        });
    }
}