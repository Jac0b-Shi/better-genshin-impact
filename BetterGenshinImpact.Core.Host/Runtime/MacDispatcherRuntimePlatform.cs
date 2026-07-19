using BetterGenshinImpact.Core.Abstractions.Recognition;
using BetterGenshinImpact.Core.Abstractions.Runtime;
using BetterGenshinImpact.Core.Script.Dependence;
using BetterGenshinImpact.GameTask;
using BetterGenshinImpact.GameTask.Model;
using BetterGenshinImpact.Platform.Abstractions;
using BetterGenshinImpact.GameTask.AutoFight;
using BetterGenshinImpact.GameTask.AutoFishing;

namespace BetterGenshinImpact.Core.Host.Runtime;

public sealed class MacDispatcherRuntimePlatform(
    CancellationToken globalCancellationToken,
    IAutoPickRuntimeState autoPickRuntimeState,
    IInputBackend inputBackend,
    Func<ISystemInfo> systemInfo,
    IAutoPickConfigProvider autoPickConfigProvider,
    IPaddleAutoPickTextRecognizer paddleRecognizer,
    IYapAutoPickTextRecognizer yapRecognizer) : IDispatcherRuntimePlatform
{
    public CancellationToken GlobalCancellationToken { get; } = globalCancellationToken;
    public int AutoWoodRoundNum => throw Unavailable("AutoWood");
    public int AutoWoodDailyMaxCount => throw Unavailable("AutoWood");
    public string AutoBossStrategyName => throw Unavailable("AutoBoss");
    public DispatcherAutoEatSettings AutoEatSettings => throw Unavailable("AutoEat");

    public void ClearTriggers() => GameTaskManager.ClearTriggers();

    public bool AddTrigger(string name, object? config)
    {
        if (!GameTaskManager.AddTrigger(
                name, config, autoPickRuntimeState, inputBackend, systemInfo(),
                autoPickConfigProvider, paddleRecognizer, yapRecognizer))
            return false;

        var trigger = GameTaskManager.TriggerDictionary![name];
        trigger.Init();
        trigger.IsEnabled = true;
        return true;
    }

    public bool GetTcgStrategy(out string content) =>
        throw Unavailable("AutoGeniusInvokation");

    public bool GetFightStrategy(string? strategyName, out string path) =>
        throw Unavailable(string.IsNullOrEmpty(strategyName) ? "AutoDomain" : "AutoBoss");

    public async Task<object?> ExecuteSoloTask(DispatcherSoloTaskRequest request,
        CancellationToken cancellationToken)
    {
        if (request is DispatcherFishingTaskRequest fishing)
        {
            await new AutoFishingTask(AutoFishingTaskParam.BuildFromSoloTaskConfig(fishing.Config))
                .Start(cancellationToken);
            return null;
        }
        throw Unavailable(request.Name);
    }

    public async Task<object?> RunParameterizedTask(string name, object parameter,
        CancellationToken cancellationToken)
    {
        if (name == "AutoFight" && parameter is AutoFightParam autoFightParam)
        {
            var factory = BetterGenshinImpact.GameTask.AutoFight.Factory.CombatTaskFactoryProvider
                .GetFactory(autoFightParam.CombatStrategyPath);
            await factory.CreateTask(autoFightParam).Start(cancellationToken);
            return null;
        }
        throw Unavailable(name);
    }

    private static CapabilityUnavailableException Unavailable(string name) => new(
        $"dispatcher task '{name}' is not composed in the macOS Core yet; no task was executed.");
}
