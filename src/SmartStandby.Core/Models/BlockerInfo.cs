namespace SmartStandby.Core.Models;

public enum BlockerType
{
    Display,
    System,
    AwayMode,
    Execution,
    PerfBoost,
    Unknown
}

public class BlockerInfo
{
    public string Name { get; set; } = string.Empty;
    public BlockerType Type { get; set; }
    public string RawDetails { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"[{Type}] {Name}";
    }
}
