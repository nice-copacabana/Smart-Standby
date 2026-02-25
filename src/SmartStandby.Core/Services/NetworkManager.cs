using SmartStandby.Core.Helpers;
using Serilog;

namespace SmartStandby.Core.Services;

public class NetworkManager
{
    private readonly PowerShellHelper _ps;
    private const string WlanInterfaceName = "Wi-Fi"; // Common default, might need auto-detection later
    private string? _lastConnectedProfile;
    private string? _detectedInterfaceName;

    public NetworkManager(PowerShellHelper ps)
    {
        _ps = ps;
    }

    private async Task<string> GetInterfaceNameAsync()
    {
        if (_detectedInterfaceName != null) return _detectedInterfaceName;
        try
        {
            string result = await _ps.ExecuteScriptAsync(
                "(netsh wlan show interfaces | Select-String 'Name\\s*:\\s*(.+)' | Select-Object -First 1 | ForEach-Object { $_.Matches[0].Groups[1].Value.Trim() }) -join ''");
            if (!string.IsNullOrWhiteSpace(result))
            {
                _detectedInterfaceName = result.Trim();
                Log.Information($"Detected Wi-Fi interface: '{_detectedInterfaceName}'");
                return _detectedInterfaceName;
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not detect Wi-Fi interface name, falling back to default.");
        }
        return WlanInterfaceName; // fallback
    }

    public async Task DisconnectWifiAsync()
    {
        Log.Information("Attempting to disconnect Wi-Fi...");
        var iface = await GetInterfaceNameAsync();

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

        await _ps.ExecuteScriptAsync($"netsh wlan disconnect interface=\"{iface}\"");
    }

    public async Task ConnectWifiAsync()
    {
        Log.Information("Attempting to connect Wi-Fi...");
        var iface = await GetInterfaceNameAsync();

        if (!string.IsNullOrWhiteSpace(_lastConnectedProfile))
        {
            Log.Information($"Reconnecting to saved profile: '{_lastConnectedProfile}'");
            await _ps.ExecuteScriptAsync($"netsh wlan connect name=\"{_lastConnectedProfile}\" interface=\"{iface}\"");
        }
        else
        {
            Log.Warning("No saved Wi-Fi profile. Falling back to auto-connect.");
            await _ps.ExecuteScriptAsync($"netsh wlan connect interface=\"{iface}\"");
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
