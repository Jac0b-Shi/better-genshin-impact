using BetterGenshinImpact.Core.Host.Transport;
using BetterGenshinImpact.Core.Recognition.OCR;
using BetterGenshinImpact.GameTask.AutoBoss;
using BetterGenshinImpact.GameTask.AutoFight;
using BetterGenshinImpact.GameTask.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace BetterGenshinImpact.Core.Host.Runtime;

public sealed class MacAutoBossRuntimePlatform(
    Func<ISystemInfo> systemInfo,
    IOcrService ocrService,
    AutoFightConfig autoFightConfig,
    ILoggerFactory loggerFactory,
    PlatformCallbackChannel callbacks,
    string sessionToken,
    CancellationToken cancellationToken) : IAutoBossRuntimePlatform
{
    public ISystemInfo SystemInfo => systemInfo();
    public IOcrService OcrService => ocrService;
    public AutoFightConfig AutoFightConfig { get; } = autoFightConfig;
    public ILogger<AutoBossTask> Logger { get; } = loggerFactory.CreateLogger<AutoBossTask>();

    public void Notify(AutoBossNotification notification, string message)
    {
        var response = callbacks.InvokeAsync("notification.emit", JObject.FromObject(new
        {
            kind = "info",
            message,
        }), sessionToken, cancellationToken).GetAwaiter().GetResult();
        if (response?.Value<bool?>("acknowledged") != true)
            throw new InvalidDataException("notification.emit did not return acknowledged=true.");
    }
}
