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
    private bool _enableNetworkDisconnect = true;

    [ObservableProperty]
    private bool _enableTdrPatch = false;

    [ObservableProperty]
    private string _newProcessName;

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
        
        // Apply System Tweaks immediately
        _tweaker.ApplyTdrPatch(EnableTdrPatch);
    }
}
