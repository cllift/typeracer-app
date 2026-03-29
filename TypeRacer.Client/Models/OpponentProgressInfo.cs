using TypeRacer.Client.ViewModels;

namespace TypeRacer.Client.Models;

public class OpponentProgressInfo : ViewModelBase
{
    private string _name = string.Empty;
    private double _progressPercent;
    private string _wpmText = "WPM: 0";
    private string _finishedText = string.Empty;

    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    public double ProgressPercent
    {
        get => _progressPercent;
        set => SetField(ref _progressPercent, value);
    }

    public string WpmText
    {
        get => _wpmText;
        set => SetField(ref _wpmText, value);
    }

    public string FinishedText
    {
        get => _finishedText;
        set => SetField(ref _finishedText, value);
    }
}