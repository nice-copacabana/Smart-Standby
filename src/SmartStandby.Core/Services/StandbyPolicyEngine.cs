using SmartStandby.Core.Models;

namespace SmartStandby.Core.Services;

public class StandbyPolicyEngine
{
    public PolicyDecision Decide(CapabilityProfile profile, SmartStandbyOptions options, bool force = false)
    {
        var decision = new PolicyDecision
        {
            TargetState = "LightSleep",
            ExplainText = "Default balanced policy.",
            ActionChain = new List<string> { "ScanBlockers", "NetworkPolicy", "Sleep" }
        };

        if (!options.Enabled)
        {
            decision.ExplainText = "Policy engine disabled by options; using legacy sleep path.";
            return decision;
        }

        if (force)
        {
            decision.Preconditions.Add("ForceMode");
        }

        // Explicit user mode overrides
        if (string.Equals(options.Mode, "Eco", StringComparison.OrdinalIgnoreCase))
        {
            if (!profile.IsAcPowered && profile.BatteryPercent > 0 && profile.BatteryPercent <= options.BatteryLowThresholdPercent)
            {
                decision.TargetState = "Hibernate";
                decision.ExplainText = $"Eco mode + low battery ({profile.BatteryPercent}%). Prefer hibernate to reduce drain.";
                decision.ActionChain = new List<string> { "ScanBlockers", "Hibernate" };
                return decision;
            }

            decision.TargetState = "LightSleep";
            decision.ExplainText = "Eco mode enabled. Prefer lighter sleep to reduce wake instability and background activity.";
            return decision;
        }

        if (string.Equals(options.Mode, "Reachability", StringComparison.OrdinalIgnoreCase))
        {
            decision.TargetState = "KeepAlive";
            decision.ExplainText = "Reachability mode enabled. Keep services online and avoid deep sleep.";
            decision.ActionChain = new List<string> { "ScanBlockers", "NetworkPolicy", "KeepAlive" };
            return decision;
        }

        // Balanced mode heuristics
        if (profile.HasS3 && profile.EthernetConnected && profile.WolLanSupported)
        {
            decision.TargetState = "DeepSleep";
            decision.ExplainText = "S3 + Ethernet + WoL-capable NIC detected. Deep sleep is likely reliable.";
            decision.Preconditions.Add("WolLanPathAvailable");
            decision.ActionChain = new List<string> { "ScanBlockers", "NetworkPolicy", "DeepSleep" };
            return decision;
        }

        if (!profile.HasS3 && profile.IsLaptop)
        {
            decision.TargetState = "KeepAlive";
            decision.ExplainText = "S0-style laptop detected. Deep sleep wake reliability may be limited; keep-alive fallback applied.";
            decision.Preconditions.Add("S0PotentialRisk");
            decision.ActionChain = new List<string> { "ScanBlockers", "NetworkPolicy", "KeepAlive" };
            return decision;
        }

        // Battery safety override in balanced mode
        if (!profile.IsAcPowered && profile.BatteryPercent > 0 && profile.BatteryPercent <= options.BatteryLowThresholdPercent)
        {
            decision.TargetState = "Hibernate";
            decision.ExplainText = $"Battery is low ({profile.BatteryPercent}%). Prefer hibernate for safety.";
            decision.ActionChain = new List<string> { "ScanBlockers", "Hibernate" };
            return decision;
        }

        return decision;
    }
}
