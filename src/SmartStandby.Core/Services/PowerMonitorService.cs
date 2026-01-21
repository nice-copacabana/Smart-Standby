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

    public void Dispose()
    {
        StopMonitoring();
    }
}
