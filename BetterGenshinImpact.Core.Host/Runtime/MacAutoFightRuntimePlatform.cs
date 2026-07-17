using BetterGenshinImpact.Core.Recognition.OCR;
using BetterGenshinImpact.Core.Recognition.ONNX;
using BetterGenshinImpact.GameTask.AutoFight;
using BetterGenshinImpact.GameTask.AutoFight.Config;
using BetterGenshinImpact.GameTask.Common;
using BetterGenshinImpact.GameTask.Model;

namespace BetterGenshinImpact.Core.Host.Runtime;

public sealed class MacAutoFightRuntimePlatform(
    ISystemInfo systemInfo,
    MacImageRegionOcrService recognition) : IAutoFightRuntimePlatform
{
    public ISystemInfo SystemInfo { get; } = systemInfo;
    public IOcrService OcrService => recognition;
    public double DpiScale => TaskControlPlatform.Current.DpiScale;
    public AutoFightConfig AutoFightConfig => throw new CapabilityUnavailableException(
        "AutoFightConfig has not been attached from the executing ScriptGroup yet.");
    public int CombatMacroPriority => throw new CapabilityUnavailableException(
        "Combat macro priority has not been initialized from Core-owned configuration yet.");
    public BgiYoloPredictor CreateYoloPredictor(BgiOnnxModel model) => recognition.CreateYoloPredictor(model);
}
