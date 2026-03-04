using Serilog;
using SmartStandby.Core.Models;

namespace SmartStandby.Core.Services;

public class WakeOrchestrator
{
    public async Task<WakeResult> TryWakeAsync(CapabilityProfile profile, SmartStandbyOptions options, CancellationToken cancellationToken = default)
    {
        var started = DateTime.UtcNow;
        cancellationToken.ThrowIfCancellationRequested();

        // Phase-1 executable fallback logic (without raw packet sender yet):
        // 1) Validate path
        // 2) Retry planning
        // 3) Emit deterministic failure codes for diagnostics/UI

        if (!profile.EthernetConnected)
        {
            return BuildFailure("WAKE_NET_NO_PATH", "Ethernet is not connected; WoL path unavailable.", started, 0, "none");
        }

        if (!profile.WolLanSupported)
        {
            return BuildFailure("WAKE_WOL_NOT_SUPPORTED", "NIC does not appear WoL-capable.", started, 0, "none");
        }

        var retries = Math.Max(1, options.WakeMaxRetries);
        var attempt = 0;

        while (attempt < retries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            attempt++;

            // Placeholder for actual magic packet sender integration.
            Log.Information("WakeOrchestrator attempt {Attempt}/{Total} (planned wol_lan)", attempt, retries);
            await Task.Delay(120, cancellationToken);
        }

        return BuildFailure(
            "WAKE_TIMEOUT",
            $"Wake attempts exhausted ({retries}) with no confirmation.",
            started,
            retries,
            "wol_lan");
    }

    private static WakeResult BuildFailure(string code, string detail, DateTime started, int attempts, string method)
    {
        return new WakeResult
        {
            Success = false,
            Method = method,
            Attempts = attempts,
            FailureCode = code,
            FailureDetail = detail,
            LatencyMs = (long)(DateTime.UtcNow - started).TotalMilliseconds
        };
    }
}
