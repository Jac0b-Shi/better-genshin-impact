using BetterGenshinImpact.Core.Host.Transport;
using BetterGenshinImpact.Core.Recognition;
using BetterGenshinImpact.Core.Recognition.OCR;
using BetterGenshinImpact.Core.Recognition.ONNX;
using BetterGenshinImpact.GameTask;
using BetterGenshinImpact.GameTask.AutoDomain;
using BetterGenshinImpact.GameTask.Model;
using BetterGenshinImpact.GameTask.Model.Area;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OpenCvSharp;

namespace BetterGenshinImpact.Core.Host.Runtime;

public sealed class MacAutoDomainRuntimePlatform(
    Func<ISystemInfo> systemInfo,
    MacImageRegionOcrService ocrService,
    ILoggerFactory loggerFactory,
    PlatformCallbackChannel callbacks,
    string sessionToken,
    CancellationToken cancellationToken) : IAutoDomainRuntimePlatform
{
    public ISystemInfo SystemInfo => systemInfo();
    public IOcrService OcrService => ocrService;
    public IStringLocalizer<AutoDomainTask> StringLocalizer =>
        TaskParameterPlatform.Current.GetStringLocalizer<AutoDomainTask>();
    public ILogger<AutoDomainTask> Logger { get; } = loggerFactory.CreateLogger<AutoDomainTask>();

    public BgiYoloPredictor CreateTreePredictor() =>
        ocrService.CreateYoloPredictor(BgiOnnxModel.BgiTree);

    public void DrawCameraDirection(ImageRegion region, double angle)
    {
        var rectangle = new Rect(region.Width / 2 - 35, 24, 70, 24);
        OverlayDrawPlatform.Current.SetLabels("AutoDomainCamera", region,
            [new OverlayLabel(rectangle, $"{angle:0} deg", true)]);
    }

    public void AddAutoEatTrigger()
    {
        if (GameTaskManager.TriggerDictionary is null ||
            !GameTaskManager.TriggerDictionary.TryGetValue("AutoEat", out var trigger))
            throw new InvalidOperationException("AutoEat trigger is unavailable before runtime initialization.");
        trigger.Init();
        trigger.IsEnabled = true;
    }

    public void Notify(AutoDomainNotification notification, string message)
    {
        var kind = notification == AutoDomainNotification.Retry ? "error" : "info";
        var response = callbacks.InvokeAsync("notification.emit", JObject.FromObject(new
        {
            kind,
            message,
        }), sessionToken, cancellationToken).GetAwaiter().GetResult();
        if (response?.Value<bool?>("acknowledged") != true)
            throw new InvalidDataException("notification.emit did not return acknowledged=true.");
    }
}
