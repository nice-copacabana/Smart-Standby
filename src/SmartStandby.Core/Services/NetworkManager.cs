using SmartStandby.Core.Helpers;
using Serilog;

namespace SmartStandby.Core.Services;

public class NetworkManager
{
    private readonly PowerShellHelper _ps;
    private const string WlanInterfaceName = "Wi-Fi"; // Common default, might need auto-detection later
    private string? _lastConnectedProfile;

    public NetworkManager(PowerShellHelper ps)
    {
        _ps = ps;
    }

    public async Task DisconnectWifiAsync()
    {
        Log.Information("Attempting to disconnect Wi-Fi...");

        // Save the currently connected profile name before disconnecting
        try
        {
            string profileResult = await _ps.ExecuteScriptAsync(
                $"(netsh wlan show interfaces | Select-String 'Profile\\s*:\\s*(.+)' | ForEach-Object {{ $_.Matches[0].Groups[1].Value.Trim() }}) -join ''");
            if (!string.IsNullOrWhiteSpace(profileResult))
            {
                _lastConnectedProfile = profileResult.Trim();
                Log.Information($"Saved Wi-Fi profile for reconnect: '{_lastConnectedProfile}'");
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not save current Wi-Fi profile name.");
        }

        await _ps.ExecuteScriptAsync($"netsh wlan disconnect interface=\"{WlanInterfaceName}\"");
    }

    public async Task ConnectWifiAsync()
    {
        Log.Information("Attempting to connect Wi-Fi...");

        if (!string.IsNullOrWhiteSpace(_lastConnectedProfile))
        {
            Log.Information($"Reconnecting to saved profile: '{_lastConnectedProfile}'");
            await _ps.ExecuteScriptAsync($"netsh wlan connect name=\"{_lastConnectedProfile}\" interface=\"{WlanInterfaceName}\"");
        }
        else
        {
            // Fallback: trigger auto-connect by enabling/disabling the adapter
            Log.Warning("No saved Wi-Fi profile. Falling back to adapter toggle to trigger auto-connect.");
            await _ps.ExecuteScriptAsync($"netsh wlan connect interface=\"{WlanInterfaceName}\"");
        }
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
