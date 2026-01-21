using Microsoft.Win32;
using Serilog;
using SmartStandby.Core.Models;
using System.Runtime.Versioning;

namespace SmartStandby.Core.Services;

[SupportedOSPlatform("windows")]
public class PowerMonitorService : IDisposable
{
    private readonly DatabaseService _db;
    private bool _isMonitoring;

    public PowerMonitorService(DatabaseService db)
    {
        _db = db;
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
            await _db.AddSessionAsync(new SleepSession 
            { 
                SleepTime = DateTime.Now, 
                IsManaged = false // Triggered by System/User, not our "Smart Sleep" button explicitly (though hard to distinguish here) 
            });
        }
        else if (e.Mode == PowerModes.Resume)
        {
            Log.Information("System is Resuming (Wake).");
            // Find the last session and update wake time
            var sessions = await _db.GetRecentSessionsAsync(1);
            var lastSession = sessions.FirstOrDefault();
            
            // If we found a recent session that hasn't been closed (WakeTime is default)
            // Or if it's very recent (e.g. within last day)
            if (lastSession != null && (lastSession.WakeTime == default || lastSession.SleepTime > DateTime.Now.AddDays(-1)))
            {
                // Note: DatabaseService currently lacks Update. 
                // Since this is MVP, we will just log it for now.
                // In a real app, we would add UpdateSessionAsync to DB Service.
                Log.Information($"Wake detected. (Ideally would update session from {lastSession.SleepTime})");
            }
        }
    }

    public void Dispose()
    {
        StopMonitoring();
    }
}
