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

    public bool KillProcess(string processName)
    {
        if (string.IsNullOrWhiteSpace(processName)) return false;
        
        // Remove .exe extension if present for matching
        processName = Path.GetFileNameWithoutExtension(processName);

        if (CriticalProcesses.Contains(processName))
        {
            Log.Warning($"ProcessGuardian blocked attempt to kill critical process: {processName}");
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
                p.WaitForExit(3000); // Wait up to 3s
            }
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex,($"Failed to kill process: {processName}"));
            return false;
        }
    }
}
