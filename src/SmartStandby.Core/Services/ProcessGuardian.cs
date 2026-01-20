using System.Diagnostics;
using Serilog;

namespace SmartStandby.Core.Services;

public class ProcessGuardian
{
    // Safety list: never kill these even if requested.
    private static readonly HashSet<string> CriticalProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "explorer", "winlogon", "csrss", "lsass", "services", "svchost", "dwm", "system", "idle"
    };

    private readonly DatabaseService _db;

    public ProcessGuardian(DatabaseService db)
    {
        _db = db;
    }

    public async Task<bool> KillProcessAsync(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName)) return false;
        
        // Remove .exe extension if present for matching
        processName = Path.GetFileNameWithoutExtension(processName);

        if (CriticalProcesses.Contains(processName) || await IsWhitelistedAsync(processName + ".exe"))
        {
            Log.Warning($"ProcessGuardian blocked attempt to kill protected process: {processName}");
            return false;
        }

        try
        {
            var processes = Process.GetProcessesByName(processName);
            if (processes.Length == 0)
            {
                Log.Information($"No running process found with name: {processName}");
                return false;
            }

            foreach (var p in processes)
            {
                Log.Information($"Killing process: {p.ProcessName} (PID: {p.Id})");
                p.Kill();
                await p.WaitForExitAsync(); 
            }
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to kill process: {processName}");
            return false;
        }
    }

    private async Task<bool> IsWhitelistedAsync(string fullProcessName)
    {
        var whitelist = await _db.GetWhitelistAsync();
        // Simple containment check. Could be improved with exact matching or regex.
        return whitelist.Any(w => string.Equals(w, fullProcessName, StringComparison.OrdinalIgnoreCase) 
                               || string.Equals(w, Path.GetFileNameWithoutExtension(fullProcessName), StringComparison.OrdinalIgnoreCase));
    }
}
