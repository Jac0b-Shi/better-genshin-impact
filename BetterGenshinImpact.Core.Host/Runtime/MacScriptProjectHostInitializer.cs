using BetterGenshinImpact.Core.Script.Dependence;
using BetterGenshinImpact.Core.Script.Project;
using BetterGenshinImpact.Core.Script.Utils;
using Microsoft.ClearScript;

namespace BetterGenshinImpact.Core.Host.Runtime;

/// <summary>
/// Registers only the upstream host objects whose complete implementations are
/// already composed on macOS. Scheduler capability remains disabled until the
/// remaining EngineExtend registrations are linked; missing names therefore
/// cannot be mistaken for successful no-op implementations.
/// </summary>
public sealed class MacScriptProjectHostInitializer : IScriptProjectHostInitializer
{
    public void Initialize(IScriptEngine engine, string workDir, string[] searchPaths, object? config)
    {
        ArgumentNullException.ThrowIfNull(engine);
        engine.AddHostObject("log", new Log());
        engine.AddHostObject("file", new LimitedFile(workDir));
        engine.AddHostObject("http", new Http());
        engine.AddHostObject("notification", new Notification());
        engine.AddHostType("ServerTime", typeof(ServerTime));
        engine.AddHostObject("strategyFile", new StrategyFile());
        engine.AddHostObject("host", new CustomHostFunctions());
        engine.AddHostType(typeof(Task));
        GlobalMethod.AddToScriptEngine(engine);

        engine.DocumentSettings.AccessFlags = DocumentAccessFlags.AllowCategoryMismatch;
        var normalizedPaths = searchPaths.Select(path => ScriptUtils.NormalizePath(workDir, path)).ToArray();
        if (normalizedPaths.Length > 0)
            engine.DocumentSettings.SearchPath = string.Join(';', normalizedPaths);
    }

    public void SetGameMetrics(int width, int height) => GlobalMethod.SetGameMetrics(width, height);
}
