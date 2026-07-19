using BetterGenshinImpact.GameTask.AutoPathing;

namespace BetterGenshinImpact.Core.Host.Runtime;

/// <summary>
/// macOS Core Host 尚未移植 AutoSkipTrigger，显式声明无自动剧情能力。
/// PathExecutor 检测到剧情界面时会记录警告并继续导航，而不是静默缺少该行为。
/// </summary>
public sealed class MacPathExecutorAutoSkipPlatform : IPathExecutorAutoSkipPlatform
{
    public IPathExecutorAutoSkipSession? CreateSession() => null;
}
