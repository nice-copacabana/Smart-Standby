using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SmartStandby.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using SmartStandby.Core.Services;

namespace SmartStandby.Views
{
    public sealed partial class DashboardPage : Page
    {
        public DashboardViewModel ViewModel { get; }
        private readonly PowerMonitorService _powerMonitor;

        public DashboardPage()
        {
            this.InitializeComponent();
            
            // In a Frame navigation scenario, we might resolve VM manually or use a ServiceLocator pattern
            // if Constructor Injection isn't natively supported by Frame.Navigate(Type).
            // For WinUI 3 + Generic Host, typical pattern is to resolve via App.Host.Services.
            ViewModel = ((App)Application.Current).Host.Services.GetRequiredService<DashboardViewModel>();
            _powerMonitor = ((App)Application.Current).Host.Services.GetRequiredService<PowerMonitorService>();
            DataContext = ViewModel;

            Loaded += OnLoadedAsync;
            Unloaded += OnUnloaded;

            _powerMonitor.ResumeReprobeRequested += PowerMonitorOnResumeReprobeRequested;
        }

        private async void OnLoadedAsync(object sender, RoutedEventArgs e)
        {
            if (ViewModel.RefreshBlockersCommand.CanExecute(null))
                await ViewModel.RefreshBlockersCommand.ExecuteAsync(null);

            if (ViewModel.RefreshPolicyCommand.CanExecute(null))
                await ViewModel.RefreshPolicyCommand.ExecuteAsync(null);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _powerMonitor.ResumeReprobeRequested -= PowerMonitorOnResumeReprobeRequested;
            Loaded -= OnLoadedAsync;
            Unloaded -= OnUnloaded;
        }

        private async void PowerMonitorOnResumeReprobeRequested(object? sender, EventArgs e)
        {
            if (ViewModel.RefreshPolicyCommand.CanExecute(null))
            {
                await ViewModel.RefreshPolicyCommand.ExecuteAsync(null);
            }
        }
    }
}
