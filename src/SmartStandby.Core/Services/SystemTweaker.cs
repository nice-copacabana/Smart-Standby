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
    public void SetRunOnStartup(bool enable)
    {
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string AppName = "SmartStandby";

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, true);
            if (key == null) return;

            if (enable)
            {
                string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
                // Determine if we are running as packaged app (MSIX) or Unpackaged
                // For Unpackaged, file path works. For MSIX, this Registry Run key methodology is NOT recommended (use StartupTask in manifest).
                // Assuming Unpackaged distribution for this task as hinted by "Unpackaged Mode" earlier.
                
                if (!string.IsNullOrEmpty(exePath) && exePath.EndsWith(".exe"))
                {
                    Log.Information($"Enabling Run on Startup for: {exePath}");
                    key.SetValue(AppName, $"\"{exePath}\"");
                }
            }
            else
            {
                Log.Information("Disabling Run on Startup");
                if (key.GetValue(AppName) != null)
                {
                    key.DeleteValue(AppName);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to change Run on Startup setting.");
        }
    }
}
