using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SmartStandby.ViewModels;
using SmartStandby.Views;
using WinRT.Interop;
using System;

namespace SmartStandby
{
    public sealed partial class MainWindow : Window
    {
        public MainWindowViewModel ViewModel { get; }

        public MainWindow()
        {
            this.InitializeComponent();
            
            ViewModel = new MainWindowViewModel();
            ViewModel.Initialize(this);
            
            // App Window & Title
            this.Title = "Smart Standby";

            // Navigate to Dashboard initially
            ContentFrame.Navigate(typeof(DashboardPage));
            NavView.SelectedItem = NavView.MenuItems[0];
        }

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
             // Ensure correct initial selection
             if (NavView.MenuItems.Count > 0)
             {
                 NavView.SelectedItem = NavView.MenuItems[0];
             }
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                 ContentFrame.Navigate(typeof(SettingsPage));
            }
            else if (args.SelectedItemContainer != null)
            {
                var tag = args.SelectedItemContainer.Tag.ToString();
                switch (tag)
                {
                    case "Dashboard":
                        ContentFrame.Navigate(typeof(DashboardPage));
                        break;
                    case "Settings": // Duplicate case if Tag is used for settings too
                        ContentFrame.Navigate(typeof(SettingsPage));
                        break;
                }
            }
        }
    }
}
