using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.Core.Infrastructure;
using BetterGenshinImpact.Core.Recognition.OCR;
using BetterGenshinImpact.Core.Recognition.ONNX;
using BetterGenshinImpact.GameTask.AutoFishing;
using BetterGenshinImpact.GameTask.Common;
using BetterGenshinImpact.GameTask.Model.Area;
using BetterGenshinImpact.GameTask.Model;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BetterGenshinImpact.Core.Host.Runtime;

public sealed class MacAutoFishingRuntimePlatform : IAutoFishingRuntimePlatform
{
    private readonly RuntimeLayout _layout;
    private readonly MacImageRegionOcrService _recognition;
    private readonly ILoggerFactory _loggerFactory;

    public MacAutoFishingRuntimePlatform(
        RuntimeLayout layout,
        ISystemInfo systemInfo,
        MacImageRegionOcrService recognition,
        ILoggerFactory loggerFactory)
    {
        _layout = layout;
        SystemInfo = systemInfo;
        _recognition = recognition;
        _loggerFactory = loggerFactory;
        (Config, GameCultureInfoName) = LoadConfig();
    }

    public ISystemInfo SystemInfo { get; }
    public AutoFishingConfig Config { get; }
    public string GameCultureInfoName { get; }
    public IOcrService OcrService => _recognition;
    public ILogger<T> GetLogger<T>() => _loggerFactory.CreateLogger<T>();
    public IStringLocalizer<T> GetStringLocalizer<T>() => new EmbeddedResourceStringLocalizer<T>();
    public BgiYoloPredictor CreateYoloPredictor(BgiOnnxModel model) =>
        _recognition.CreateYoloPredictor(model);
    public bool IsGameActive(out string activeProcessName)
    {
        TaskControlPlatform.Current.EnsureGameActive();
        activeProcessName = SystemInfo.GameProcessName;
        return true;
    }
    public ImageRegion? CaptureFrame() => TaskControlPlatform.Current.CaptureToRectArea(forceNew: true);
    public void DisableRealtimeFishing()
    {
        Config.Enabled = false;
        PersistConfig();
    }
    public Task SetTimeAsync(int hour, int minute, CancellationToken cancellationToken) =>
        throw new CapabilityUnavailableException(
            "AutoFishing time adjustment is not composed until the shared SetTimeTask closure is linked.");
    public void SaveBehaviourScreenshot(ImageRegion imageRegion, string fileName) =>
        throw new CapabilityUnavailableException(
            "AutoFishing behavior screenshots require the Core-owned UID-cover configuration to be composed.");

    private (AutoFishingConfig Config, string Culture) LoadConfig()
    {
        var path = Path.Combine(_layout.UserPath, "config.json");
        if (!File.Exists(path)) return (new AutoFishingConfig(), "zh-Hans");
        var root = JsonNode.Parse(File.ReadAllText(path), documentOptions: new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip,
        }) as JsonObject ?? throw new InvalidDataException("User/config.json root must be an object.");
        var config = root["autoFishingConfig"]?.Deserialize<AutoFishingConfig>(ConfigJson.Options)
            ?? new AutoFishingConfig();
        var culture = root["otherConfig"]?["gameCultureInfoName"]?.GetValue<string>() ?? "zh-Hans";
        _ = System.Globalization.CultureInfo.GetCultureInfo(culture);
        return (config, culture);
    }

    private void PersistConfig()
    {
        var path = Path.Combine(_layout.UserPath, "config.json");
        var root = File.Exists(path)
            ? JsonNode.Parse(File.ReadAllText(path), documentOptions: new JsonDocumentOptions
              {
                  AllowTrailingCommas = true,
                  CommentHandling = JsonCommentHandling.Skip,
              }) as JsonObject
                ?? throw new InvalidDataException("User/config.json root must be an object.")
            : new JsonObject();
        root["autoFishingConfig"] = JsonSerializer.SerializeToNode(Config, ConfigJson.Options);
        Directory.CreateDirectory(_layout.UserPath);
        var temporaryPath = path + ".tmp";
        File.WriteAllText(temporaryPath, root.ToJsonString(ConfigJson.Options));
        File.Move(temporaryPath, path, overwrite: true);
    }
}
