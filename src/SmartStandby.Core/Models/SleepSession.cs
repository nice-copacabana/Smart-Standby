using SQLite;

namespace SmartStandby.Core.Models;

public class SleepSession
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public DateTime SleepTime { get; set; }
    public DateTime WakeTime { get; set; }

    /// <summary>
    /// Calculated duration in minutes
    /// </summary>
    public double DurationMinutes => (WakeTime - SleepTime).TotalMinutes;

    /// <summary>
    /// The source that triggered the wake (e.g., "PowerBtn", "Mouse")
    /// </summary>
    public string WakeSource { get; set; } = string.Empty;

    /// <summary>
    /// Battery level percentage when going to sleep
    /// </summary>
    public int BatteryStart { get; set; }

    /// <summary>
    /// Battery level percentage upon waking up
    /// </summary>
    public int BatteryEnd { get; set; }

    /// <summary>
    /// Was this session explicitly handled by our logic?
    /// </summary>
    public bool IsManaged { get; set; }
}
