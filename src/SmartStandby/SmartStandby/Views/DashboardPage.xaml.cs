using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SmartStandby.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace SmartStandby.Views
{
    public sealed partial class DashboardPage : Page
    {
        public DashboardViewModel ViewModel { get; }

        public DashboardPage()
        {
            this.InitializeComponent();
            
            // In a Frame navigation scenario, we might resolve VM manually or use a ServiceLocator pattern
            // if Constructor Injection isn't natively supported by Frame.Navigate(Type).
            // For WinUI 3 + Generic Host, typical pattern is to resolve via App.Host.Services.
            ViewModel = ((App)Application.Current).Host.Services.GetRequiredService<DashboardViewModel>();
            DataContext = ViewModel;

            Loaded += async (s, e) => 
            {
                if (ViewModel.RefreshBlockersCommand.CanExecute(null))
                    await ViewModel.RefreshBlockersCommand.ExecuteAsync(null);
            };
        }
    }
}
