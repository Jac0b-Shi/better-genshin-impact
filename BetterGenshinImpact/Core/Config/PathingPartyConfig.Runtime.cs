using BetterGenshinImpact.GameTask;

namespace BetterGenshinImpact.Core.Config;

/// <summary>
/// 依赖已初始化任务上下文的默认配置工厂。拆分文件不改变 Windows 行为，
/// 便于其他 Host 使用自身 composition root 提供运行时配置。
/// </summary>
public partial class PathingPartyConfig
{
    public static PathingPartyConfig BuildDefault()
    {
        // 即便是不启用的情况下也设置默认值，减少后续使用的判断
        var pathingConditionConfig = TaskContext.Instance().Config.PathingConditionConfig;
        return new PathingPartyConfig
        {
            OnlyInTeleportRecover = pathingConditionConfig.OnlyInTeleportRecover,
            UseGadgetIntervalMs = pathingConditionConfig.UseGadgetIntervalMs,
            AutoEatEnabled = pathingConditionConfig.AutoEatEnabled
        };
    }
}
