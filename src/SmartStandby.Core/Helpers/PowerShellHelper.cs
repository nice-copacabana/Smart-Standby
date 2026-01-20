using System.Management.Automation;
using System.Collections.ObjectModel;
using System.Text;
using Serilog;

namespace SmartStandby.Core.Helpers;

public class PowerShellHelper
{
    public async Task<string> ExecuteScriptAsync(string script)
    {
        try
        {
            using var ps = PowerShell.Create();
            ps.AddScript(script);

            var checkAsync = ps.BeginInvoke();
            var results = await Task.Factory.FromAsync(checkAsync, ps.EndInvoke);

            var stringBuilder = new StringBuilder();
            
            // Collect Output
            foreach (var result in results)
            {
                stringBuilder.AppendLine(result.ToString());
            }

            // Collect Errors
            if (ps.Streams.Error.Count > 0)
            {
                foreach (var error in ps.Streams.Error)
                {
                    Log.Warning($"PowerShell Error: {error}");
                }
            }

            return stringBuilder.ToString().Trim();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to execute PowerShell script");
            return string.Empty;
        }
    }
}
