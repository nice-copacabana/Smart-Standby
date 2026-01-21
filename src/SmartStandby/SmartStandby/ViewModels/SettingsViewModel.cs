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
    public partial string NewProcessName { get; set; } = string.Empty;

    public ObservableCollection<string> WhitelistProcesses { get; } = new();

    public SettingsViewModel(DatabaseService databaseService, SystemTweaker tweaker)
    {
        _databaseService = databaseService;
        _tweaker = tweaker;
        LoadSettings();
    }

    private async void LoadSettings()
    {
        EnableNetworkDisconnect = await _databaseService.GetConfigBoolAsync("EnableNetworkDisconnect", true);
        EnableTdrPatch = await _databaseService.GetConfigBoolAsync("EnableTdrPatch", false);
        EnableRunOnStartup = await _databaseService.GetConfigBoolAsync("EnableRunOnStartup", false);

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
    private async Task SaveSettingsAsync()
    {
        await _databaseService.SetConfigBoolAsync("EnableNetworkDisconnect", EnableNetworkDisconnect);
        await _databaseService.SetConfigBoolAsync("EnableTdrPatch", EnableTdrPatch);
        await _databaseService.SetConfigBoolAsync("EnableRunOnStartup", EnableRunOnStartup);
        
        // Apply System Tweaks immediately
        _tweaker.ApplyTdrPatch(EnableTdrPatch);
        _tweaker.SetRunOnStartup(EnableRunOnStartup);
    }
}
