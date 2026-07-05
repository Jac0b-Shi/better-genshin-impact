using BetterGenshinImpact.Core.Abstractions.Runtime;

namespace BetterGenshinImpact.Core.Adapters;

/// <summary>
/// Immutable initial runtime state placeholder for AutoPick coordination.
/// Provides a construction-time <c>StopCount</c> value.
/// No threading logic, no static gateway, no bare Thread.
/// Mutation/coordination will be introduced when a real macOS pause/resume owner exists.
/// </summary>
public sealed class MacAutoPickRuntimeState : IAutoPickRuntimeState
{
    private readonly int _stopCount;

    public MacAutoPickRuntimeState(int stopCount = 0)
    {
        _stopCount = stopCount;
    }

    public int StopCount => _stopCount;
}
