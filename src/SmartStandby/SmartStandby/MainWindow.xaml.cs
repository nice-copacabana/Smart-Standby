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
using Windows.Foundation;
using Windows.Foundation.Collections;
using SmartStandby.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SmartStandby
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindowViewModel ViewModel => (MainWindowViewModel) ((FrameworkElement)Content).DataContext;

        public MainWindow(MainWindowViewModel viewModel)
        {
            this.InitializeComponent();
            ((FrameworkElement)Content).DataContext = viewModel;
            
            // Auto-refresh on load
            if (Content is FrameworkElement root)
            {
                root.Loaded += async (s, e) => 
                {
                    if (viewModel.RefreshBlockersCommand.CanExecute(null))
                        await viewModel.RefreshBlockersCommand.ExecuteAsync(null);
                };
            }
        }
        
        // Default constructor for XAML previewer fallback (optional, but good practice if needed)
        // However, with DI, we usually rely on the DI container calling the parameterized constructor.
        // WinUI Xaml Compiler sometimes strictly requires a default constructor if used as resource.
        // But for MainWindow, it's usually fine.
    }
}
