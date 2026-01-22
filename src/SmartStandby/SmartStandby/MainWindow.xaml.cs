using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SmartStandby.ViewModels;
using SmartStandby.Views;
using WinRT.Interop;
using System;
using System.Runtime.InteropServices;
using SmartStandby.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using SmartStandby.Core.Services;
using Microsoft.Extensions.DependencyInjection;

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

            // Initialize System Tray (P/Invoke)
            _trayService = new SmartStandby.Services.TrayIconService(WinRT.Interop.WindowNative.GetWindowHandle(this));

            // Navigate to Dashboard initially
            ContentFrame.Navigate(typeof(DashboardPage));
            NavView.SelectedItem = NavView.MenuItems[0];

            InterceptMessages();
        }

        private SmartStandby.Services.TrayIconService _trayService;
        private IntPtr _oldWndProc = IntPtr.Zero;
        private Win32Native.WndProc? _newWndProc;

        private void InterceptMessages()
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(this);
            _newWndProc = new Win32Native.WndProc(NewWindowProc);
            _oldWndProc = Win32Native.SetWindowLongPtr(hwnd, Win32Native.GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(_newWndProc));
        }

        private IntPtr NewWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == TrayIconService.WM_TRAYICON)
            {
                uint trayMsg = (uint)(lParam.ToInt64() & 0xFFFF);
                if (trayMsg == TrayIconService.WM_LBUTTONDBLCLK)
                {
                    RestoreWindow();
                }
                else if (trayMsg == TrayIconService.WM_RBUTTONUP)
                {
                    ShowTrayMenu();
                }
            }
            return Win32Native.CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
        }

        private void RestoreWindow()
        {
            this.Activate();
            var appWindow = GetAppWindow();
            appWindow.Show();

            // Stop Backpack Guard if it was running
            var powerMonitor = ((App)App.Current).Host.Services.GetRequiredService<PowerMonitorService>();
            powerMonitor.ResetWatchdog();
        }

        private void ShowTrayMenu()
        {
            // Simplified for now: just restore or sleep
            // In a real app we'd use TrackPopupMenu
            RestoreWindow();
        }

        private AppWindow GetAppWindow()
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            return AppWindow.GetFromWindowId(windowId);
        }

        private void MainWindow_Closed(object sender, WindowEventArgs args)
        {
             _trayService?.Dispose();
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
