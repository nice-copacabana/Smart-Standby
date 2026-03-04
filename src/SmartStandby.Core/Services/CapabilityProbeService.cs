using System.Net.NetworkInformation;
using SmartStandby.Core.Helpers;
using SmartStandby.Core.Models;

namespace SmartStandby.Core.Services;

public class CapabilityProbeService
{
    private readonly PowerShellHelper _ps;

    public CapabilityProbeService(PowerShellHelper ps)
    {
        _ps = ps;
    }

    public async Task<CapabilityProfile> ProbeAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var profile = new CapabilityProfile();

        // 1) Sleep state capability from powercfg /a
        try
        {
            var output = await _ps.ExecuteScriptAsync("powercfg /a");
            var text = output?.ToLowerInvariant() ?? string.Empty;

            profile.HasS0 = text.Contains("standby (s0") || text.Contains("s0 low power idle");
            profile.HasS3 = text.Contains("standby (s3)");
            profile.HasHibernate = text.Contains("hibernate");
        }
        catch
        {
            // Keep defaults when probe fails.
        }

        // 2) Network capability/context via NetworkInterface
        try
        {
            var all = NetworkInterface.GetAllNetworkInterfaces();
            var active = all.Where(n => n.OperationalStatus == OperationalStatus.Up).ToList();

            profile.EthernetConnected = active.Any(n => n.NetworkInterfaceType == NetworkInterfaceType.Ethernet);
            profile.WifiConnected = active.Any(n => n.NetworkInterfaceType == NetworkInterfaceType.Wireless80211);

            // 3) Basic WoL LAN heuristic
            profile.WolLanSupported = all.Any(n =>
                n.NetworkInterfaceType == NetworkInterfaceType.Ethernet &&
                (n.Description.Contains("intel", StringComparison.OrdinalIgnoreCase) ||
                 n.Description.Contains("realtek", StringComparison.OrdinalIgnoreCase)));
        }
        catch
        {
            // Keep defaults when probe fails.
        }

        // 4) Power/battery context via Win32 helper
        try
        {
            if (Win32Utils.GetSystemPowerStatus(out var status))
            {
                profile.IsLaptop = status.BatteryFlag != 128;
                profile.IsAcPowered = status.ACLineStatus == 1;
                profile.BatteryPercent = status.BatteryLifePercent == 255 ? -1 : status.BatteryLifePercent;
            }
        }
        catch
        {
            // Keep defaults when probe fails.
        }

        if (!profile.HasS3 && profile.IsLaptop)
        {
            profile.RiskHint = "S0-only device: deep sleep wake reliability may be limited.";
        }

        return profile;
    }
}
