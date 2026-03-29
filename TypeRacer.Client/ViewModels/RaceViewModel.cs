using System;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace TypeRacer.Client.ViewModels;

public class RaceViewModel : ViewModelBase
{
    private string _countdownText = string.Empty;
    private string _statusMessage = "Get ready...";
    private string _raceText = "The quick brown fox jumps over the lazy dog.";
    private string _typedText = string.Empty;
    private string _wpmText = "WPM: 0";
    private bool _isCountdownVisible = true;
    private bool _isRaceVisible = false;
    private DateTime? _raceStartTime;

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
        set => SetField(ref _raceText, value);
    }

    public string TypedText
    {
        get => _typedText;
        set
        {
            if (SetField(ref _typedText, value))
            {
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

    public RaceViewModel(int countdownSeconds)
    {
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

    private void UpdateTypingState()
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

        if (minutesElapsed > 0)
        {
            int wordsTyped = correctCharacters / 5;
            int wpm = (int)Math.Round(wordsTyped / minutesElapsed);
            WpmText = $"WPM: {wpm}";
        }

        if (TypedText == RaceText)
        {
            StatusMessage = "Finished!";
        }
        else if (TypedText.Length > 0)
        {
            int lastIndex = TypedText.Length - 1;

            if (lastIndex < RaceText.Length && TypedText[lastIndex] == RaceText[lastIndex])
            {
                StatusMessage = "Correct";
            }
            else
            {
                StatusMessage = "Incorrect";
            }
        }
    }
}