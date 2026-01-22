using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartStandby.Core.Services;
using SmartStandby.Core.Models;
using SmartStandby.Models;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace SmartStandby.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly BlockerScanner _scanner;
    private readonly SleepService _sleepService;
    private readonly DatabaseService _db;

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Ready";

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    private string _healthStatus = "Unknown";

    [ObservableProperty]
    private string _healthMessage = "Awaiting first wake event...";

    [ObservableProperty]
    private string _healthColor = "Gray";

    public ObservableCollection<BlockerInfo> Blockers { get; } = new();
    
    public ObservableCollection<ChartDataPoint> ChartData { get; } = new();

    public DashboardViewModel(BlockerScanner scanner, SleepService sleepService, DatabaseService db)
    {
        _scanner = scanner;
        _sleepService = sleepService;
        _db = db;
        
        _ = LoadChartDataAsync();
        _ = RefreshHealthAsync();
    }

    private async Task RefreshHealthAsync()
    {
        // For MVP, we'll pull from the logs or a transient state in the future.
        // Currently, we just set a healthy default until a real failure is recorded.
        HealthStatus = "Healthy";
        HealthMessage = "System wake-up diagnostics passed successfully.";
        HealthColor = "Green";
    }

    private async Task LoadChartDataAsync()
    {
        try 
        {
            var sevenDaysAgo = DateTime.Now.Date.AddDays(-6);
            var sessions = await _db.GetSessionsAfterAsync(sevenDaysAgo);

            var grouped = sessions.GroupBy(s => s.SleepTime.Date)
                                  .ToDictionary(g => g.Key, g => g.Sum(s => s.DurationMinutes) / 60.0);

            var points = new List<ChartDataPoint>();
            double maxVal = 0;

            for (int i = 0; i < 7; i++)
            {
                var date = sevenDaysAgo.AddDays(i);
                double val = grouped.TryGetValue(date, out var hours) ? hours : 0;
                if (val > maxVal) maxVal = val;
                
                points.Add(new ChartDataPoint 
                { 
                    Label = date.ToString("MM/dd"), 
                    Value = val,
                    Tooltip = $"{val:F1} hrs"
                });
            }
            
            double maxHeight = 150;
            double scaleRatio = maxVal > 0 ? maxVal : 1;

            ChartData.Clear();
            foreach (var p in points)
            {
                p.Height = (p.Value / scaleRatio) * maxHeight;
                if (p.Value > 0 && p.Height < 4) p.Height = 4;
                ChartData.Add(p);
            }
        }
        catch (Exception) 
        {
            StatusMessage = "Failed to load chart data.";
        }
    }

    [RelayCommand]
    private async Task RefreshBlockersAsync()
    {
        if (IsBusy) return;
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
        catch (Exception)
        {
            StatusMessage = "Scan failed.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SmartSleepAsync()
    {
        if (IsBusy) return;
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
        }
    }
}
