using BetterGenshinImpact.Core.Abstractions.Runtime;

namespace BetterGenshinImpact.Core.Adapters;

/// <summary>
/// macOS runtime state for AutoPick coordination.
/// Simple mutable state — no threading logic, no static gateway.
/// </summary>
public sealed class MacAutoPickRuntimeState : IAutoPickRuntimeState
{
    private volatile int _stopCount;

    public MacAutoPickRuntimeState(int stopCount = 0)
    {
        _stopCount = stopCount;
    }

    public int StopCount => _stopCount;
}
