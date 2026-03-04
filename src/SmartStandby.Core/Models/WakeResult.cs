namespace SmartStandby.Core.Models;

public sealed class WakeResult
{
    public bool Success { get; set; }
    public string Method { get; set; } = "none";
    public int Attempts { get; set; }
    public long LatencyMs { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureDetail { get; set; }
}
