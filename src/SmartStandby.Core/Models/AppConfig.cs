using SQLite;

namespace SmartStandby.Core.Models;

public class AppConfig
{
    [PrimaryKey]
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}
