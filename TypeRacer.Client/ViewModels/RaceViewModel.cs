using System.Threading.Tasks;
using Avalonia.Threading;

namespace TypeRacer.Client.ViewModels;

public class RaceViewModel : ViewModelBase
{
    private string _countdownText;
    private string _statusMessage = "Get ready...";

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

    public RaceViewModel(int countdownSeconds)
    {
        CountdownText = countdownSeconds.ToString();
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
        StatusMessage = "Race has started.";

        // We will replace this later with the real typing screen.
    }
}