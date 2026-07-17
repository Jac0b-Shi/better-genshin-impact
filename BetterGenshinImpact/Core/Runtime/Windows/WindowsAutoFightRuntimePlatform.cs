using BetterGenshinImpact.Core.Recognition.OCR;
using BetterGenshinImpact.Core.Recognition.ONNX;
using BetterGenshinImpact.GameTask.AutoFight;
using BetterGenshinImpact.GameTask.AutoFight.Config;
using BetterGenshinImpact.GameTask;
using BetterGenshinImpact.GameTask.Model;
using Microsoft.Extensions.DependencyInjection;

namespace BetterGenshinImpact.Core.Runtime.Windows;

public sealed class WindowsAutoFightRuntimePlatform : IAutoFightRuntimePlatform
{
    public ISystemInfo SystemInfo => TaskContext.Instance().SystemInfo;
    public AutoFightConfig AutoFightConfig => TaskContext.Instance().Config.AutoFightConfig;
    public IOcrService OcrService => OcrFactory.Paddle;
    public double DpiScale => TaskContext.Instance().DpiScale;
    public int CombatMacroPriority => TaskContext.Instance().Config.MacroConfig.CombatMacroPriority;
    public BgiYoloPredictor CreateYoloPredictor(BgiOnnxModel model) =>
        App.ServiceProvider.GetRequiredService<BgiOnnxFactory>().CreateYoloPredictor(model);
}
