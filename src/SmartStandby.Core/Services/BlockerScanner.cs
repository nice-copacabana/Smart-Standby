using SmartStandby.Core.Helpers;
using SmartStandby.Core.Models;
using System.Text.RegularExpressions;

namespace SmartStandby.Core.Services;

public class BlockerScanner
{
    private readonly PowerShellHelper _ps;

    public BlockerScanner(PowerShellHelper ps)
    {
        _ps = ps;
    }

    public async Task<List<BlockerInfo>> ScanAsync()
    {
        var output = await _ps.ExecuteScriptAsync("powercfg /requests");
        return ParsePowerCfgOutput(output);
    }

    private List<BlockerInfo> ParsePowerCfgOutput(string output)
    {
        var blockers = new List<BlockerInfo>();
        var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        BlockerType currentType = BlockerType.Unknown;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            // Check for section headers
            if (trimmed.StartsWith("DISPLAY:")) { currentType = BlockerType.Display; continue; }
            if (trimmed.StartsWith("SYSTEM:")) { currentType = BlockerType.System; continue; }
            if (trimmed.StartsWith("AWAYMODE:")) { currentType = BlockerType.AwayMode; continue; }
            if (trimmed.StartsWith("EXECUTION:")) { currentType = BlockerType.Execution; continue; }
            if (trimmed.StartsWith("PERFBOOST:")) { currentType = BlockerType.PerfBoost; continue; }

            // If it's "None.", skip it
            if (trimmed.Equals("None.", StringComparison.OrdinalIgnoreCase)) continue;

            // Otherwise, it's a blocker under the current section
            if (currentType != BlockerType.Unknown && !string.IsNullOrWhiteSpace(trimmed))
            {
                // Simple parsing: Use the whole line as the name for now.
                // Powercfg often outputs the driver name or process path.
                blockers.Add(new BlockerInfo
                {
                    Name = trimmed,
                    Type = currentType,
                    RawDetails = trimmed
                });
            }
        }

        return blockers;
    }
}
