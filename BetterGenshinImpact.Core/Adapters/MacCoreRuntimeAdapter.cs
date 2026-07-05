using System.Globalization;
using BetterGenshinImpact.Core.Abstractions.Runtime;
using BetterGenshinImpact.Core.Recognition;
using BetterGenshinImpact.GameTask.AutoPick;

namespace BetterGenshinImpact.Core.Adapters;

/// <summary>
/// macOS runtime adapter providing AutoPick config and OCR config.
/// Receives configuration values explicitly — no reference to TaskContext, RunnerContext, or Windows APIs.
/// <para>
/// <c>AutoPickConfig</c> returns the same mutable reference passed at construction,
/// preserving upstream write-back semantics (e.g. <c>PickKey = "F"</c> on load failure).
/// </para>
/// </summary>
public sealed class MacCoreRuntimeAdapter : IAutoPickConfigProvider, IOcrRuntimeConfigProvider
{
    private readonly AutoPickConfig _autoPickConfig;
    private readonly PaddleOcrModelConfig _paddleModel;
    private readonly string _gameCultureInfoName;

    public MacCoreRuntimeAdapter(
        AutoPickConfig autoPickConfig,
        PaddleOcrModelConfig paddleModel,
        string gameCultureInfoName)
    {
        _autoPickConfig = autoPickConfig ?? throw new ArgumentNullException(nameof(autoPickConfig));
        _paddleModel = paddleModel;
        _gameCultureInfoName = gameCultureInfoName
            ?? throw new ArgumentNullException(nameof(gameCultureInfoName));
    }

    // IAutoPickConfigProvider
    public AutoPickConfig AutoPickConfig => _autoPickConfig;

    // IOcrRuntimeConfigProvider
    public PaddleOcrModelConfig PaddleModel => _paddleModel;
    public string GameCultureInfoName => _gameCultureInfoName;
}
