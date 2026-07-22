using BetterGenshinImpact.Core.Abstractions.Runtime;
using BetterGenshinImpact.Core.Host.Transport;
using BetterGenshinImpact.Core.Recognition.OCR;
using BetterGenshinImpact.GameTask.AutoArtifactSalvage;
using BetterGenshinImpact.GameTask.AutoStygianOnslaught;
using BetterGenshinImpact.GameTask.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace BetterGenshinImpact.Core.Host.Runtime;

public sealed class MacAutoStygianOnslaughtRuntimePlatform(
    Func<ISystemInfo> systemInfo,
    IOcrService ocrService,
    IAutoPickConfigProvider autoPickConfigProvider,
    ILoggerFactory loggerFactory,
    PlatformCallbackChannel callbacks,
    string sessionToken,
    CancellationToken cancellationToken) : IAutoStygianOnslaughtRuntimePlatform
{
    public ISystemInfo SystemInfo => systemInfo();
    public IOcrService OcrService { get; } = ocrService;
    public string PickKey => autoPickConfigProvider.AutoPickConfig.PickKey;
    public ILogger<AutoStygianOnslaughtTask> Logger { get; } =
        loggerFactory.CreateLogger<AutoStygianOnslaughtTask>();

    public void Notify(AutoStygianOnslaughtNotification notification, string message)
    {
        var response = callbacks.InvokeAsync("notification.emit", JObject.FromObject(new
        {
            kind = "info",
            message,
        }), sessionToken, cancellationToken).GetAwaiter().GetResult();
        if (response?.Value<bool?>("acknowledged") != true)
            throw new InvalidDataException("notification.emit did not return acknowledged=true.");
    }

    public Task RunArtifactSalvage(
        AutoArtifactSalvageTaskParam parameter, CancellationToken taskCancellationToken) =>
        new AutoArtifactSalvageTask(
                parameter,
                OcrService,
                SystemInfo.AssetScale,
                loggerFactory.CreateLogger<AutoArtifactSalvageTask>())
            .Start(taskCancellationToken);
}
