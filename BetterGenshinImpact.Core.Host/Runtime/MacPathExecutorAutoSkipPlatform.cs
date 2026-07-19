using BetterGenshinImpact.GameTask.AutoPathing;

namespace BetterGenshinImpact.Core.Host.Runtime;

/// <summary>
/// macOS Core Host 尚未组合 AutoSkipTrigger 时显式失败，禁止 PathExecutor 跳过行为后继续成功。
/// </summary>
public sealed class MacPathExecutorAutoSkipPlatform : IPathExecutorAutoSkipPlatform
{
    public IPathExecutorAutoSkipSession CreateSession() =>
        throw new CapabilityUnavailableException(
            "PathExecutor AutoSkip requires the full shared AutoSkipTrigger closure.");
}
