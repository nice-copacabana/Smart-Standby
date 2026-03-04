namespace SmartStandby.Core.Models;

public sealed class SmartStandbyOptions
{
    public bool Enabled { get; set; } = true;
    public string Mode { get; set; } = "Balanced";
    public bool AutoProbeOnStartup { get; set; } = true;
    public int ProbeIntervalMinutes { get; set; } = 360;

    public int BatteryLowThresholdPercent { get; set; } = 20;
    public int ConsecutiveWakeFailToDowngrade { get; set; } = 3;

    public bool EnablePolicyExplain { get; set; } = true;
    public bool EnableAutoDowngrade { get; set; } = true;

    public int WakeTimeoutMs { get; set; } = 45000;
    public int WakeMaxRetries { get; set; } = 2;
}
