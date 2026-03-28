using Avalonia.Controls;
using Avalonia.Controls.Templates;
using TypeRacer.Client.ViewModels;
using TypeRacer.Client.Views;

namespace TypeRacer.Client;

public class ViewLocator : IDataTemplate
{
    public Control? Build(object? data)
    {
        return data switch
        {
            JoinViewModel => new JoinView(),
            LobbyViewModel => new LobbyView(),
            RaceViewModel => new RaceView(),
            _ => new TextBlock { Text = $"No view found for {data?.GetType().Name}" }
        };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}