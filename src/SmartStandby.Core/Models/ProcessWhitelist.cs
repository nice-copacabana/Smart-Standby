using SQLite;

namespace SmartStandby.Core.Models;

public class ProcessWhitelist
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed(Unique = true)]
    public string ProcessName { get; set; } = string.Empty;
}
