using Microsoft.Win32;
using Serilog;
using System.Runtime.Versioning;

namespace SmartStandby.Core.Services;

[SupportedOSPlatform("windows")]
public class SystemTweaker
{
    private const string GraphicsDriversKey = @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers";
    private const string TdrDelayValueName = "TdrDelay";
    private const int TdrDelaySeconds = 8;

    public void ApplyTdrPatch(bool enable)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(GraphicsDriversKey, true);
            if (key == null)
            {
                Log.Error($"Registry key not found: {GraphicsDriversKey}");
                return;
            }

            if (enable)
            {
                Log.Information($"Applying TDR Patch: Setting TdrDelay to {TdrDelaySeconds}s");
                key.SetValue(TdrDelayValueName, TdrDelaySeconds, RegistryValueKind.DWord);
            }
            else
            {
                Log.Information("Removing TDR Patch (Deleting TdrDelay value)");
                if (key.GetValue(TdrDelayValueName) != null)
                {
                    key.DeleteValue(TdrDelayValueName);
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            Log.Error("Access Denied when writing to Registry. Please run as Administrator.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to apply TDR Patch.");
        }
    }
}
