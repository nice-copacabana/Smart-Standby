using Microsoft.Win32;
using Serilog;
using SmartStandby.Core.Models;
using SmartStandby.Core.Helpers;
using System.Runtime.Versioning;

namespace SmartStandby.Core.Services;

[SupportedOSPlatform("windows")]
public class PowerMonitorService : IDisposable
{
    private readonly DatabaseService _db;
    private readonly SleepService _sleepService;
    private readonly PowerShellHelper _ps;
    private bool _isMonitoring;
    private System.Threading.Timer? _backpackGuardTimer;
    private System.Threading.Timer? _automationTimer;
    
    public event EventHandler<string>? NotificationRequested;

    public PowerMonitorService(DatabaseService db, SleepService sleepService, PowerShellHelper ps)
    {
        _db = db;
        _sleepService = sleepService;
        _ps = ps;
    }

    public void StartMonitoring()
    {
        if (_isMonitoring) return;

        try
        {
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
            _isMonitoring = true;
            
            // Start Automation Timer (every 1 minute)
            _automationTimer = new System.Threading.Timer(async _ => await CheckAutomationRules(), null, TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(1));

            Log.Information("Power Monitoring Started. Listening for Sleep/Wake events.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to subscribe to PowerModeChanged events.");
        }
    }

    public void StopMonitoring()
    {
        if (!_isMonitoring) return;

        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        _automationTimer?.Dispose();
        _isMonitoring = false;
        Log.Information("Power Monitoring Stopped.");
    }

    private async void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        Log.Information($"System Power Event Detected: {e.Mode}");

        if (e.Mode == PowerModes.Suspend)
        {
            Log.Information("System is Suspending (Sleep).");
            
            // Deduplication: Check if a managed session was created in the last 15 seconds
            var sessions = await _db.GetRecentSessionsAsync(1);
            var lastSession = sessions.FirstOrDefault();
            
            if (lastSession != null && lastSession.IsManaged && (DateTime.Now - lastSession.SleepTime).TotalSeconds < 15)
            {
                Log.Information("Managed sleep session detected recently. Skipping duplicate log.");
            }
            else
            {
                await _db.AddSessionAsync(new SleepSession 
                { 
                    SleepTime = DateTime.Now, 
                    IsManaged = false 
                });
            }
        }
        else if (e.Mode == PowerModes.Resume)
        {
            Log.Information("System is Resuming (Wake).");
            
            // Start Backpack Guard Watchdog
            StartBackpackGuard();

            // Run Health Check after 5 seconds
            _ = Task.Delay(5000).ContinueWith(_ => CheckWakeupHealthAsync());

            // Find the last session and update wake time
            var sessions = await _db.GetRecentSessionsAsync(1);
            var lastSession = sessions.FirstOrDefault();
            
            // Update if it's an open session (WakeTime is default)
            if (lastSession != null && lastSession.WakeTime == default(DateTime))
            {
                lastSession.WakeTime = DateTime.Now;
                await _db.UpdateSessionAsync(lastSession);
                Log.Information($"Session Updated. Sleep: {lastSession.SleepTime}, Wake: {lastSession.WakeTime}");
            }
        }
    }

    private void StartBackpackGuard()
    {
        // 20 minutes timeout for inactive wake
        const int timeoutMinutes = 20;
        _backpackGuardTimer?.Dispose();
        _backpackGuardTimer = new System.Threading.Timer(async _ => 
        {
            Log.Information("Backpack Guard: Check triggered.");
            // For now, we simply force hibernate if the timer fires.
            // In a more advanced version, we would check GetLastInputInfo to see if user actually touched the PC.
            await _sleepService.HibernateAsync();
        }, null, TimeSpan.FromMinutes(timeoutMinutes), System.Threading.Timeout.InfiniteTimeSpan);
        
        Log.Information($"Backpack Guard Watchdog started. System will hibernate in {timeoutMinutes}m if no managed cancellation occurs.");
    }

    private void StopBackpackGuard()
    {
        _backpackGuardTimer?.Dispose();
        _backpackGuardTimer = null;
    }

    public void ResetWatchdog()
    {
        Log.Information("Backpack Guard: Watchdog stopped due to user activity.");
        StopBackpackGuard();
    }

    private async Task CheckWakeupHealthAsync()
    {
        Log.Information("Performing Wake-up Health Check... Querying Event Logs.");
        
        // System Event IDs to check:
        // 41: Kernel-Power (Unexpected shutdown)
        // 107: Kernel-Power (System resume)
        // 109: Kernel-Power (System shutdown triggered)
        // 506: Kernel-Power (Modern Standby entering low power - helpful for S0 systems)
        
        string script = @"
            $fiveMinutesAgo = (Get-Date).AddMinutes(-10)
            $events = Get-WinEvent -FilterHashtable @{LogName='System'; ID=41, 109, 506; StartTime=$fiveMinutesAgo} -ErrorAction SilentlyContinue
            if ($events) { $events.Count } else { 0 }
        ";

        try 
        {
            string result = await _ps.ExecuteScriptAsync(script);
            if (int.TryParse(result.Trim(), out int errorCount))
            {
                // 2. Battery Drain Check
                var sessions = await _db.GetRecentSessionsAsync(1);
                var lastSession = sessions.FirstOrDefault();
                double drainRate = lastSession?.DrainRate ?? 0;
                
                string thresholdStr = await _db.GetConfigAsync("BatteryDrainThreshold", "3.0");
                double.TryParse(thresholdStr, out double threshold);

                if (errorCount > 0)
                {
                    Log.Warning($"Wake-up Health Check: Detected {errorCount} critical power events/errors.");
                    await SaveHealthStatusAsync("Warning", $"Detected {errorCount} power errors in logs.");
                }
                else if (drainRate > threshold)
                {
                    Log.Warning($"Wake-up Health Check: High battery drain detected ({drainRate:F1}%/h > {threshold:F1}%/h).");
                    await SaveHealthStatusAsync("Warning", $"Standby drain too high: {drainRate:F1}%/h.");
                }
                else
                {
                    Log.Information("Wake-up Health Check Completed: Status Healthy.");
                    await SaveHealthStatusAsync("Healthy", "System status healthy and power efficient.");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to perform real wake-up health check.");
            await SaveHealthStatusAsync("Error", "Failed to perform health check.");
        }
    }

    private async Task SaveHealthStatusAsync(string status, string message)
    {
        var sessions = await _db.GetRecentSessionsAsync(1);
        var lastSession = sessions.FirstOrDefault();
        if (lastSession != null)
        {
            lastSession.HealthStatus = status;
            lastSession.HealthMessage = message;
            await _db.UpdateSessionAsync(lastSession);
            Log.Information($"Health Status persisted to session {lastSession.Id}.");
        }
    }

    public void Dispose()
    {
        StopMonitoring();
    }

    private async Task CheckAutomationRules()
    {
        try
        {
            // check power status
            var powerStatus = Win32Utils.GetSystemPowerStatus();
            bool isCharging = powerStatus.ACLineStatus == 1;
            int batteryPercent = powerStatus.BatteryLifePercent;

            // 1. Low Battery Trigger
            bool enableLowBatt = await _db.GetConfigBoolAsync("EnableLowBatteryTrigger", false);
            if (enableLowBatt && !isCharging && batteryPercent > 0)
            {
                string thresholdStr = await _db.GetConfigAsync("LowBatteryThreshold", "20");
                if (int.TryParse(thresholdStr, out int threshold) && batteryPercent <= threshold)
                {
                    Log.Warning($"Automation Access: Low Battery detected ({batteryPercent}% <= {threshold}%). Triggering sleep.");
                    NotificationRequested?.Invoke(this, $"Low Battery ({batteryPercent}%). Entering Smart Sleep...");
                    await Task.Delay(3000); // Give user a moment to see notification
                    await _sleepService.SmartSleepAsync();
                    return;
                }
            }

            // 2. Scheduled Sleep
            bool enableSchedule = await _db.GetConfigBoolAsync("EnableScheduledSleep", false);
            if (enableSchedule)
            {
                string timeStr = await _db.GetConfigAsync("ScheduledSleepTime", "23:00:00");
                if (TimeSpan.TryParse(timeStr, out TimeSpan scheduleTime))
                {
                    var now = DateTime.Now.TimeOfDay;
                    // Trigger if within the same minute
                    if (now >= scheduleTime && now < scheduleTime.Add(TimeSpan.FromMinutes(1)))
                    {
                         Log.Information($"Automation Access: Scheduled time reached ({scheduleTime}). Triggering sleep.");
                         NotificationRequested?.Invoke(this, "Scheduled Sleep time reached. Entering Smart Sleep...");
                         await Task.Delay(3000);
                         await _sleepService.SmartSleepAsync();
                         
                         // Pause timer briefly to avoid double trigger in same minute? 
                         // Actually the sleep will suspend the system, so next tick will be after wake.
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in Automation Check.");
        }
    }
}
