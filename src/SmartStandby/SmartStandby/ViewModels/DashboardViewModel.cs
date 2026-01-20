using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartStandby.Core.Services;
using SmartStandby.Core.Models;
using System.Collections.ObjectModel;

namespace SmartStandby.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly BlockerScanner _scanner;
    private readonly SleepService _sleepService;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isBusy;

    public ObservableCollection<BlockerInfo> Blockers { get; } = new();

    public DashboardViewModel(BlockerScanner scanner, SleepService sleepService)
    {
        _scanner = scanner;
        _sleepService = sleepService;
    }

    [RelayCommand]
    private async Task RefreshBlockersAsync()
    {
        IsBusy = true;
        StatusMessage = "Scanning for blockers...";
        
        try
        {
            Blockers.Clear();
            var results = await _scanner.ScanAsync();
            foreach (var item in results)
            {
                Blockers.Add(item);
            }
            StatusMessage = $"Found {results.Count} blockers.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Scan failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SmartSleepAsync()
    {
        IsBusy = true;
        StatusMessage = "Initiating Smart Sleep...";

        try
        {
            bool success = await _sleepService.ExecuteSmartSleepAsync(force: true);
            if (success)
            {
                StatusMessage = "System is going to sleep...";
            }
            else
            {
                StatusMessage = "Sleep trigger failed (or cancelled).";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Sleep error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            // In a real scenario, execution might pause here during sleep,
            // or the app might be suspended.
        }
    }
}
