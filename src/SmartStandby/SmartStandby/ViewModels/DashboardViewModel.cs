using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartStandby.Core.Services;
using SmartStandby.Core.Models;
using SmartStandby.Models;
using System.Collections.ObjectModel;
using System.Linq; // Ensure Linq is available

namespace SmartStandby.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly BlockerScanner _scanner;
    private readonly SleepService _sleepService;
    private readonly DatabaseService _db;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private bool _isBusy;

    public ObservableCollection<BlockerInfo> Blockers { get; } = new();
    
    // Custom Chart Data
    public ObservableCollection<ChartDataPoint> ChartData { get; } = new();

    public DashboardViewModel(BlockerScanner scanner, SleepService sleepService, DatabaseService db)
    {
        _scanner = scanner;
        _sleepService = sleepService;
        _db = db;
        
        LoadChartDataAsync();
    }

    private async void LoadChartDataAsync()
    {
        try 
        {
            var sevenDaysAgo = DateTime.Now.Date.AddDays(-6);
            var sessions = await _db.GetSessionsAfterAsync(sevenDaysAgo);

            // Group by Date and Sum Duration
            var grouped = sessions.GroupBy(s => s.SleepTime.Date)
                                  .ToDictionary(g => g.Key, g => g.Sum(s => s.DurationMinutes) / 60.0);

            var points = new List<ChartDataPoint>();
            double maxVal = 0;

            for (int i = 0; i < 7; i++)
            {
                var date = sevenDaysAgo.AddDays(i);
                double val = grouped.ContainsKey(date) ? grouped[date] : 0;
                if (val > maxVal) maxVal = val;
                
                points.Add(new ChartDataPoint 
                { 
                    Label = date.ToString("MM/dd"), 
                    Value = val,
                    Tooltip = $"{val:F1} hrs"
                });
            }
            
            // Normalize Height (Max 150px)
            double maxHeight = 150;
            if (maxVal == 0) maxVal = 1; // Prevent div by zero using 1 hr as baseline if empty

            ChartData.Clear();
            foreach (var p in points)
            {
                p.Height = (p.Value / maxVal) * maxHeight;
                // Min height for visibility
                if (p.Value > 0 && p.Height < 4) p.Height = 4;
                
                ChartData.Add(p);
            }
        }
        catch (Exception ex) 
        {
            StatusMessage = "Failed to load chart data.";
        }
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
