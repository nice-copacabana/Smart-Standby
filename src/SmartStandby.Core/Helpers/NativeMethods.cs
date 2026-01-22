using System.Runtime.InteropServices;

namespace SmartStandby.Core.Helpers;

public static class NativeMethods
{
    // P/Invoke for managing power state
    [DllImport("powrprof.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.I1)]
    private static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);

    /// <summary>
    /// Puts the system into sleep (S3) or Hibernate (S4).
    /// </summary>
    /// <param name="hibernate">True for Hibernate, False for Sleep/Standby.</param>
    /// <returns>True if successful.</returns>
    public static bool TriggerSleep(bool hibernate = false)
    {
        // ForceCritical=false allows applications to request permission (though we might want true later).
        // DisableWakeEvent=false allows wake timers/events to wake the PC.
        return SetSuspendState(hibernate, false, false);
    }

    public static bool TriggerHibernate() => SetSuspendState(true, false, false);
}
