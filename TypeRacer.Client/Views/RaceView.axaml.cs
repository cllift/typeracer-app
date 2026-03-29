using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Threading;
using TypeRacer.Client.ViewModels;

namespace TypeRacer.Client.Views;

public partial class RaceView : UserControl
{
    public RaceView()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is RaceViewModel vm)
        {
            vm.PropertyChanged -= OnViewModelPropertyChanged;
            vm.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is RaceViewModel vm && e.PropertyName == nameof(RaceViewModel.IsRaceVisible))
        {
            if (vm.IsRaceVisible)
            {
                Dispatcher.UIThread.Post(() =>
                {
                     HiddenInputBox.Focus();
                }, DispatcherPriority.Input);
            }
        }
    }
}