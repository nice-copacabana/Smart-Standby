namespace SmartStandby.Core.Models;

public sealed class PolicyDecision
{
    public string TargetState { get; set; } = "LightSleep";
    public List<string> ActionChain { get; set; } = new();
    public List<string> Preconditions { get; set; } = new();
    public string ExplainText { get; set; } = string.Empty;
}
