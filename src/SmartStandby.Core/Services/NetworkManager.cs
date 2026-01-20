using SmartStandby.Core.Helpers;
using Serilog;

namespace SmartStandby.Core.Services;

public class NetworkManager
{
    private readonly PowerShellHelper _ps;
    private const string WlanInterfaceName = "Wi-Fi"; // Common default, might need auto-detection later

    public NetworkManager(PowerShellHelper ps)
    {
        _ps = ps;
    }

    public async Task DisconnectWifiAsync()
    {
        Log.Information("Attempting to disconnect Wi-Fi...");
        await _ps.ExecuteScriptAsync($"netsh wlan disconnect interface=\"{WlanInterfaceName}\"");
    }

    public async Task ConnectWifiAsync()
    {
        Log.Information("Attempting to connect Wi-Fi...");
        // Attempts to connect to the last known profile
        await _ps.ExecuteScriptAsync($"netsh wlan connect name=CurrentProfile interface=\"{WlanInterfaceName}\"");
        // Note: 'netsh wlan connect' usually requires a specific profile name. 
        // For a robust 'Resume' feature, we might need to store the profile name before disconnecting.
        // For Day 2 MVP, we will try a simple 'connect' which usually uses auto-connect logic if available, 
        // or we can refine this to capture the profile logic later.
    }
    
    /// <summary>
    ///  Re-enables the adapter (Alternative to netsh disconnect is disable-netadapter)
    /// </summary>
    public async Task DisableAdapterAsync()
    {
         // Requires Admin
         await _ps.ExecuteScriptAsync($"Disable-NetAdapter -Name \"{WlanInterfaceName}\" -Confirm:$false");
    }

    public async Task EnableAdapterAsync()
    {
         // Requires Admin
         await _ps.ExecuteScriptAsync($"Enable-NetAdapter -Name \"{WlanInterfaceName}\" -Confirm:$false");
    }
}
