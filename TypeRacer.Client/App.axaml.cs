using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using TypeRacer.Client.Services;
using TypeRacer.Client.ViewModels;
using TypeRacer.Client.Views;

namespace TypeRacer.Client;

public partial class App : Application
{
    public static NetworkClient NetworkClient { get; } = new NetworkClient();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}