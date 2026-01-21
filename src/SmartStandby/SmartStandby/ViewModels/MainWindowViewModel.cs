using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;

namespace SmartStandby.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private Microsoft.UI.Xaml.Window? _window;

    public void Initialize(Microsoft.UI.Xaml.Window window)
    {
        _window = window;
    }

    [RelayCommand]
    private void ShowWindow()
    {
        _window?.Activate();
    }

    [RelayCommand]
    private void ExitApplication()
    {
        Application.Current.Exit();
    }
}
