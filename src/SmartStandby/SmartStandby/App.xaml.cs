using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SmartStandby
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;
        public IHost Host { get; }

        public App()
        {
            InitializeComponent();

            Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
                .UseContentRoot(AppContext.BaseDirectory)
                .ConfigureServices((context, services) =>
                {
                    // Core Helpers
                    services.AddTransient<SmartStandby.Core.Helpers.PowerShellHelper>();

                    // Core Services
                    services.AddSingleton<SmartStandby.Core.Services.DatabaseService>();
                    services.AddSingleton<SmartStandby.Core.Services.ProcessGuardian>();
                    services.AddSingleton<SmartStandby.Core.Services.PowerMonitorService>();
                    services.AddTransient<SmartStandby.Core.Services.SystemTweaker>();
                    
                    services.AddTransient<SmartStandby.Core.Services.BlockerScanner>();
                    services.AddTransient<SmartStandby.Core.Services.NetworkManager>();
                    services.AddTransient<SmartStandby.Core.Services.SleepService>();
                    services.AddSingleton<HttpClient>();
                    services.AddTransient<SmartStandby.Core.Services.UpdateService>();

                    // ViewModels
                    services.AddTransient<SmartStandby.ViewModels.DashboardViewModel>();
                    services.AddTransient<SmartStandby.ViewModels.SettingsViewModel>();
                    
                    // Windows
                    services.AddTransient<MainWindow>();
                })
                .UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
                    .ReadFrom.Configuration(context.Configuration)
                    .Enrich.FromLogContext()
                    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day))
                .Build();
        }

        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            await Host.StartAsync();

            // Initialize DB on startup
            var db = Host.Services.GetRequiredService<SmartStandby.Core.Services.DatabaseService>();
            await db.InitializeAsync();

            // Start Power Monitoring
            var powerMonitor = Host.Services.GetRequiredService<SmartStandby.Core.Services.PowerMonitorService>();
            powerMonitor.StartMonitoring();

            // Resolve Main Window with DI
            _window = Host.Services.GetRequiredService<MainWindow>();
            _window.Activate();
        }
    }
}
