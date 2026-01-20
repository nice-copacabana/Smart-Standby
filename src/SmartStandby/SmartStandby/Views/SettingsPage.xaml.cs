using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SmartStandby.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace SmartStandby.Views
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsViewModel ViewModel { get; }

        public SettingsPage()
        {
            this.InitializeComponent();
            this.Name = "RootPage";
            ViewModel = ((App)Application.Current).Host.Services.GetRequiredService<SettingsViewModel>();
            DataContext = ViewModel;
        }
    }
}
