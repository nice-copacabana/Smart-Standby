using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SmartStandby.Views;

namespace SmartStandby
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            
            // Navigate to Dashboard initially
            ContentFrame.Navigate(typeof(DashboardPage));
            NavView.SelectedItem = NavView.MenuItems[0];
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                 // We disabled built-in settings item to use our own custom one, 
                 // but if enabled, we would navigate here.
            }
            else
            {
                var tag = args.InvokedItemContainer.Tag.ToString();
                switch (tag)
                {
                    case "Dashboard":
                        ContentFrame.Navigate(typeof(DashboardPage));
                        break;
                    case "Settings":
                        ContentFrame.Navigate(typeof(SettingsPage));
                        break;
                }
            }
        }
    }
}
