using Serilog;
using SmartStandby.Core.Helpers;
using SmartStandby.Core.Models;

namespace SmartStandby.Core.Services;

public class SleepService
{
    private readonly BlockerScanner _scanner;
    private readonly NetworkManager _network;
    private readonly ProcessGuardian _guardian;
    private readonly DatabaseService _db;

    public SleepService(BlockerScanner scanner, NetworkManager network, ProcessGuardian guardian, DatabaseService db)
    {
        _scanner = scanner;
        _network = network;
        _guardian = guardian;
        _db = db;
    }

    /// <summary>
    /// Executes the smart sleep sequence.
    /// </summary>
    /// <param name="force">If true, ignores specific blockers or forces kill.</param>
    public async Task<bool> ExecuteSmartSleepAsync(bool force = false)
    {
        Log.Information("Starting Smart Sleep Sequence...");

        // 1. Detect Blockers
        var blockers = await _scanner.ScanAsync();
        if (blockers.Any())
        {
            Log.Information($"Detected {blockers.Count} blockers.");
            
            // Fetch Whitelist
            var whitelist = await _db.GetWhitelistAsync();
            var whitelistSet = new HashSet<string>(whitelist, StringComparison.OrdinalIgnoreCase);

            foreach (var b in blockers)
            {
                Log.Information($"Blocker: {b.Name} [{b.Type}]");

                // Logic: If force is enabled, try to kill blocking processes
                if (force && !string.IsNullOrWhiteSpace(b.Name) && b.Name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    if (whitelistSet.Contains(b.Name))
                    {
                        Log.Information($"Skipping whitelist process: {b.Name}");
                        continue;
                    }

                    Log.Information($"Force mode enabled. Attempting to kill blocker: {b.Name}");
                    await _guardian.KillProcessAsync(b.Name); 
                }
            }
        }

        // 2. Network Silence
        bool disconnectNetwork = await _db.GetConfigBoolAsync("EnableNetworkDisconnect", true);
        if (disconnectNetwork)
        {
            try 
            {
                await _network.DisconnectWifiAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to disconnect network. Proceeding anyway.");
            }
        }

        // 3. Record Session Start
        var session = new SleepSession
        {
            SleepTime = DateTime.Now,
            IsManaged = true,
            BatteryStart = GetBatteryPercentage()
        };
        await _db.AddSessionAsync(session);

        // 4. Trigger Sleep
        Log.Information("Triggering System Sleep (S3)...");
        bool success = NativeMethods.TriggerSleep(); // This call blocks until wake on some systems, or returns immediately on others.
        // On modern Windows, SetSuspendState returns immediately usually.
        // We might need to listen to PowerModeChanged events for true wake detection.
        
        if (!success)
        {
            Log.Error("Failed to trigger sleep via Native API.");
            return false;
        }

        // 5. Post-Wake Logic (This might run immediately if SetSuspendState is non-blocking, making this tricky)
        // For MVP, we assume we want to restore network "eventually". 
        // Real-implementation: We should Hook SystemEvents.PowerModeChanged in the UI layer/Background Service
        // and call RestoreNetwork() from there.
        // For this function, strictly speaking, we just instigated sleep.
        
        return true;
    }

    public async Task HibernateAsync()
    {
        Log.Information("Backpack Guard: Triggering forced Hibernation due to inactivity after wake.");
        NativeMethods.TriggerHibernate();
    }

    public async Task WakeUpAsync()
    {
        Log.Information("System Waking Up. Restoring services...");
        await _network.ConnectWifiAsync();
        
        var recentSession = (await _db.GetRecentSessionsAsync(1)).FirstOrDefault();
        if (recentSession != null && recentSession.WakeTime == default)
        {
           recentSession.WakeTime = DateTime.Now;
           recentSession.BatteryEnd = GetBatteryPercentage();
           // Update DB
           await _db.UpdateSessionAsync(recentSession);
        }
    }

    private int GetBatteryPercentage()
    {
        try
        {
            var status = new SYSTEM_POWER_STATUS();
            if (GetSystemPowerStatus(out status))
            {
                if (status.BatteryLifePercent != 255)
                {
                    return status.BatteryLifePercent;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to get battery status.");
        }
        return -1; // Unknown
    }

    [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemPowerStatus(out SYSTEM_POWER_STATUS lpSystemPowerStatus);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct SYSTEM_POWER_STATUS
    {
        public byte ACLineStatus;
        public byte BatteryFlag;
        public byte BatteryLifePercent;
        public byte SystemStatusFlag;
        public int BatteryLifeTime;
        public int BatteryFullLifeTime;
    }
}
