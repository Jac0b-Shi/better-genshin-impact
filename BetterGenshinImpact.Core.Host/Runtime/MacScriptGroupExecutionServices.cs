using BetterGenshinImpact.GameTask.AutoPathing;
using BetterGenshinImpact.GameTask.FarmingPlan;

namespace BetterGenshinImpact.Core.Host.Runtime;

public sealed class MacScriptGroupExecutionServices : IScriptGroupExecutionServices
{
    public BetterGenshinImpact.Core.Config.PathingPartyConfig DefaultPartyConfig =>
        throw new CapabilityUnavailableException(
            "Default Pathing party configuration is unavailable until PathExecutor is composed.");

    public IPathExecutor CreatePathExecutor(CancellationToken cancellationToken) =>
        throw new CapabilityUnavailableException(
            "The full upstream PathExecutor dependency closure is not composed on macOS yet.");

    public void AddAutoPickTrigger() => throw new CapabilityUnavailableException(
        "Pathing AutoPick trigger composition is unavailable until PathExecutor is composed.");

    public PathingFailurePolicy PathingFailurePolicy => throw new CapabilityUnavailableException(
        "Pathing failure policy is unavailable until PathExecutor is composed.");

    public void RecordFarmingSession(FarmingSession session, FarmingRouteInfo route) =>
        throw new CapabilityUnavailableException(
            "Farming statistics persistence is unavailable until PathExecutor is composed.");
}
