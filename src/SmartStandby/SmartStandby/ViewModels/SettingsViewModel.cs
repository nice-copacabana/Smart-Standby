using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartStandby.Core.Services;
using SmartStandby.Core.Models;
using System.Collections.ObjectModel;

namespace SmartStandby.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly DatabaseService _databaseService;
    private readonly SystemTweaker _tweaker;
    private readonly UpdateService _updateService;

    [ObservableProperty]
    private string _currentVersion = "1.0.0";

    [ObservableProperty]
    private string? _latestVersion;

    [ObservableProperty]
    private bool _isUpdateAvailable;

    [ObservableProperty]
    private bool _isCheckingForUpdates;

    [ObservableProperty]
    private string _updateStatusText = string.Empty;

    [ObservableProperty]
    public partial bool EnableNetworkDisconnect { get; set; } = true;
    partial void OnEnableNetworkDisconnectChanged(bool value) => SaveSettingsCommand.Execute(null);

    [ObservableProperty]
    public partial bool EnableTdrPatch { get; set; } = false;
    partial void OnEnableTdrPatchChanged(bool value) => SaveSettingsCommand.Execute(null);

    [ObservableProperty]
    public partial bool EnableRunOnStartup { get; set; } = false;
    partial void OnEnableRunOnStartupChanged(bool value) => SaveSettingsCommand.Execute(null);

    [ObservableProperty]
    public partial double BatteryDrainThreshold { get; set; } = 3.0;
    partial void OnBatteryDrainThresholdChanged(double value) => SaveSettingsCommand.Execute(null);

    [ObservableProperty]
    public partial bool EnableLowBatteryTrigger { get; set; } = false;
    partial void OnEnableLowBatteryTriggerChanged(bool value) => SaveSettingsCommand.Execute(null);

    [ObservableProperty]
    public partial int LowBatteryThreshold { get; set; } = 20;
    partial void OnLowBatteryThresholdChanged(int value) => SaveSettingsCommand.Execute(null);

    [ObservableProperty]
    public partial bool EnableScheduledSleep { get; set; } = false;
    partial void OnEnableScheduledSleepChanged(bool value) => SaveSettingsCommand.Execute(null);

    [ObservableProperty]
    public partial TimeSpan ScheduledSleepTime { get; set; } = new TimeSpan(23, 0, 0);
    partial void OnScheduledSleepTimeChanged(TimeSpan value) => SaveSettingsCommand.Execute(null);

    [ObservableProperty]
    public partial string NewProcessName { get; set; } = string.Empty;

    public ObservableCollection<string> WhitelistProcesses { get; } = new();

    [RelayCommand]
    public async Task ClearHistoryAsync()
    {
        await _databaseService.ClearSessionsAsync();
        // Notify user?
        UpdateStatusText = "Session history cleared.";
    }

    [RelayCommand]
    public void OpenLogsFolder()
    {
        try
        {
            string logFolder = Path.Combine(AppContext.BaseDirectory, "logs");
            if (Directory.Exists(logFolder))
            {
                System.Diagnostics.Process.Start("explorer.exe", logFolder);
            }
            else
            {
                UpdateStatusText = "Logs folder not found.";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to open logs folder.");
            UpdateStatusText = $"Error opening logs folder: {ex.Message}";
        }
    }

    public SettingsViewModel(DatabaseService databaseService, SystemTweaker tweaker, UpdateService updateService)
    {
        _databaseService = databaseService;
        _tweaker = tweaker;
        _updateService = updateService;
        
        CurrentVersion = _updateService.GetCurrentVersion();
        LoadSettings();
    }

    private async void LoadSettings()
    {
        EnableNetworkDisconnect = await _databaseService.GetConfigBoolAsync("EnableNetworkDisconnect", true);
        EnableTdrPatch = await _databaseService.GetConfigBoolAsync("EnableTdrPatch", false);
        EnableRunOnStartup = await _databaseService.GetConfigBoolAsync("EnableRunOnStartup", false);
        
        string thresholdStr = await _databaseService.GetConfigAsync("BatteryDrainThreshold", "3.0");
        if (double.TryParse(thresholdStr, out double threshold))
            BatteryDrainThreshold = threshold;

        EnableLowBatteryTrigger = await _databaseService.GetConfigBoolAsync("EnableLowBatteryTrigger", false);
        
        string lowBattStr = await _databaseService.GetConfigAsync("LowBatteryThreshold", "20");
        if (int.TryParse(lowBattStr, out int lowBatt))
            LowBatteryThreshold = lowBatt;

        EnableScheduledSleep = await _databaseService.GetConfigBoolAsync("EnableScheduledSleep", false);

        string scheduleStr = await _databaseService.GetConfigAsync("ScheduledSleepTime", "23:00:00");
        if (TimeSpan.TryParse(scheduleStr, out TimeSpan time))
            ScheduledSleepTime = time;

        var list = await _databaseService.GetWhitelistAsync();
        WhitelistProcesses.Clear();
        foreach (var item in list)
        {
            WhitelistProcesses.Add(item);
        }
    }

    [RelayCommand]
    private async Task AddWhitelistAsync()
    {
        if (!string.IsNullOrWhiteSpace(NewProcessName) && !WhitelistProcesses.Contains(NewProcessName))
        {
            await _databaseService.AddWhitelistAsync(NewProcessName);
            WhitelistProcesses.Add(NewProcessName);
            NewProcessName = string.Empty;
        }
    }

    [RelayCommand]
    private async Task RemoveWhitelistAsync(string name)
    {
        if (WhitelistProcesses.Contains(name))
        {
            await _databaseService.RemoveWhitelistAsync(name);
            WhitelistProcesses.Remove(name);
        }
    }

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        if (IsCheckingForUpdates) return;

        IsCheckingForUpdates = true;
        UpdateStatusText = "Checking for updates...";
        IsUpdateAvailable = false;

        try
        {
            var (updateAvailable, latestVersion, downloadUrl) = await _updateService.CheckForUpdatesAsync();
            
            if (updateAvailable)
            {
                IsUpdateAvailable = true;
                LatestVersion = latestVersion;
                UpdateStatusText = $"New version available: {latestVersion}";
            }
            else
            {
                UpdateStatusText = "Your application is up to date.";
            }
        }
        catch (Exception ex)
        {
            UpdateStatusText = $"Error checking for updates: {ex.Message}";
        }
        finally
        {
            IsCheckingForUpdates = false;
        }
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        await _databaseService.SetConfigBoolAsync("EnableNetworkDisconnect", EnableNetworkDisconnect);
        await _databaseService.SetConfigBoolAsync("EnableTdrPatch", EnableTdrPatch);
        await _databaseService.SetConfigBoolAsync("EnableRunOnStartup", EnableRunOnStartup);
        await _databaseService.SetConfigAsync("BatteryDrainThreshold", BatteryDrainThreshold.ToString("F1"));
        
        await _databaseService.SetConfigBoolAsync("EnableLowBatteryTrigger", EnableLowBatteryTrigger);
        await _databaseService.SetConfigAsync("LowBatteryThreshold", LowBatteryThreshold.ToString());
        await _databaseService.SetConfigBoolAsync("EnableScheduledSleep", EnableScheduledSleep);
        await _databaseService.SetConfigAsync("ScheduledSleepTime", ScheduledSleepTime.ToString());
        
        // Apply System Tweaks immediately
        _tweaker.ApplyTdrPatch(EnableTdrPatch);
        _tweaker.SetRunOnStartup(EnableRunOnStartup);
    }
}
