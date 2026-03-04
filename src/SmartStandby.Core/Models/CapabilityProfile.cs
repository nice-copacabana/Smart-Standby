namespace SmartStandby.Core.Models;

public sealed class CapabilityProfile
{
    public bool HasS0 { get; set; }
    public bool HasS3 { get; set; }
    public bool HasHibernate { get; set; }

    public bool EthernetConnected { get; set; }
    public bool WifiConnected { get; set; }

    public bool WolLanSupported { get; set; }
    public bool WolLanConfigured { get; set; }
    public bool WoWlanSupported { get; set; }

    public bool IsLaptop { get; set; }
    public bool IsAcPowered { get; set; }
    public int BatteryPercent { get; set; } = -1;

    public double WakeSuccessRate7d { get; set; }
    public string? RiskHint { get; set; }
}
