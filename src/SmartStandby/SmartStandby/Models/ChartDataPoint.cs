namespace SmartStandby.Models;

public class ChartDataPoint
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
    public double Height { get; set; }
    public string Tooltip { get; set; } = string.Empty;
}
