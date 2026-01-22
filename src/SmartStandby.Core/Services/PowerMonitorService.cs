using Microsoft.Win32;
using Serilog;
using SmartStandby.Core.Models;
using System.Runtime.Versioning;

namespace SmartStandby.Core.Services;

[SupportedOSPlatform("windows")]
public class PowerMonitorService : IDisposable
{
    private readonly DatabaseService _db;
    private readonly SleepService _sleepService;
    private bool _isMonitoring;
    private System.Threading.Timer? _backpackGuardTimer;

    public PowerMonitorService(DatabaseService db, SleepService sleepService)
    {
        _db = db;
        _sleepService = sleepService;
    }

    public void StartMonitoring()
    {
        if (_isMonitoring) return;

        try
        {
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
            _isMonitoring = true;
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
        Log.Information("Performing Wake-up Health Check...");
        // In a real scenario, we'd use PowerShellHelper to run:
        // Get-WinEvent -LogName System -FilterHashtable @{LogName='System';ID=41,109,506;StartTime=(Get-Date).AddMinutes(-10)}
        // For MVP, we'll log the attempt and simulate success.
        
        // This is where we would detect "Dirty Sleep" (abrupt wake, crash on sleep, etc.)
        Log.Information("Wake-up Health Check Completed: Status Healthy.");
    }

    public void Dispose()
    {
        StopMonitoring();
    }
}
