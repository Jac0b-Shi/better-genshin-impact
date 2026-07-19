using System;
using System.Threading;
using System.Threading.Tasks;
using BetterGenshinImpact.GameTask.AutoFishing;
using BetterGenshinImpact.GameTask;
using BetterGenshinImpact.GameTask.Common.Job;
using BetterGenshinImpact.GameTask.Model;
using BetterGenshinImpact.ViewModel.Pages;

namespace BetterGenshinImpact.Core.Script.Dependence;

public sealed class WindowsGenshinRuntimePlatform : IGenshinRuntimePlatform
{
    public ISystemInfo SystemInfo => TaskContext.Instance().SystemInfo;
    public double DpiScale => TaskContext.Instance().DpiScale;
    public string MapMatchingMethod => TaskContext.Instance().Config.PathingConditionConfig.MapMatchingMethod;
    public AutoFishingTaskParam BuildAutoFishingTaskParam()
    {
        var viewModel = App.GetService<TaskSettingsPageViewModel>()
            ?? throw new InvalidOperationException("TaskSettingsPageViewModel is unavailable.");
        return AutoFishingTaskParam.BuildFromConfig(
            TaskContext.Instance().Config.AutoFishingConfig, viewModel.SaveScreenshotOnKeyTick);
    }
    public Task<CraftMaterialResult> CraftMaterial(string materialName, int quantity,
        string? materialType, CancellationToken cancellationToken) =>
        new CraftMaterialTask(materialName, quantity, materialType).Start(cancellationToken);
    public Task ClaimBattlePassRewards(CancellationToken cancellationToken) =>
        new ClaimBattlePassRewardsTask().Start(cancellationToken);
    public Task GoToCraftingBench(string country, CancellationToken cancellationToken) =>
        new GoToCraftingBenchTask().Start(country, cancellationToken);
}
