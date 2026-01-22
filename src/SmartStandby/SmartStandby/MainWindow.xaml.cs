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
        private readonly PowerMonitorService _powerMonitor;

        public MainWindow(PowerMonitorService powerMonitor)
        {
            this.InitializeComponent();
            
            _powerMonitor = powerMonitor;
            _powerMonitor.NotificationRequested += (s, msg) => _trayService.ShowNotification("Smart Standby Automation", msg);

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
        private Win32Utils.WndProc? _newWndProc;

        private void InterceptMessages()
        {
            IntPtr hwnd = WindowNative.GetWindowHandle(this);
            _newWndProc = new Win32Utils.WndProc(NewWindowProc);
            _oldWndProc = Win32Utils.SetWindowLongPtr(hwnd, Win32Utils.GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(_newWndProc));
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
            return Win32Utils.CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
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
            IntPtr hwnd = WindowNative.GetWindowHandle(this);
            IntPtr hMenu = Win32Utils.CreatePopupMenu();
            
            Win32Utils.AppendMenu(hMenu, Win32Utils.MF_STRING, Win32Utils.ID_TRAY_OPEN, "Open Smart Standby");
            Win32Utils.AppendMenu(hMenu, Win32Utils.MF_STRING, Win32Utils.ID_TRAY_SLEEP, "Sleep Now");
            Win32Utils.AppendMenu(hMenu, Win32Utils.MF_STRING, 0, "-"); // Separator
            Win32Utils.AppendMenu(hMenu, Win32Utils.MF_STRING, Win32Utils.ID_TRAY_EXIT, "Exit");

            Win32Utils.POINT pt;
            Win32Utils.GetCursorPos(out pt);

            // TrackPopupMenu with TPM_RETURNCMD returns the ID of the selected item
            Win32Utils.SetForegroundWindow(hwnd);
            uint command = (uint)Win32Utils.TrackPopupMenu(hMenu, Win32Utils.TPM_LEFTALIGN | Win32Utils.TPM_RETURNCMD, pt.X, pt.Y, 0, hwnd, IntPtr.Zero);
            Win32Utils.DestroyMenu(hMenu);

            HandleTrayCommand(command);
        }

        private async void HandleTrayCommand(uint commandId)
        {
            switch (commandId)
            {
                case Win32Utils.ID_TRAY_OPEN:
                    RestoreWindow();
                    break;
                case Win32Utils.ID_TRAY_SLEEP:
                    var sleepService = ((App)App.Current).Host.Services.GetRequiredService<SleepService>();
                    await sleepService.ExecuteSmartSleepAsync(force: true);
                    break;
                case Win32Utils.ID_TRAY_EXIT:
                    Application.Current.Exit();
                    break;
            }
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
