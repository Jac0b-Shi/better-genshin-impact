using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.Core.Host.Transport;
using BetterGenshinImpact.Core.Script.Dependence;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BetterGenshinImpact.Core.Host.Runtime;

public sealed class NotificationSettingsCatalog(
    RuntimeLayout layout,
    PlatformCallbackChannel callbacks,
    string sessionToken,
    CancellationToken cancellationToken)
{
    private readonly object _lock = new();
    private MacScriptHostServices? _scriptHostServices;

    public void AttachScriptHostServices(MacScriptHostServices services)
    {
        _scriptHostServices = services ?? throw new ArgumentNullException(nameof(services));
        services.SetJsNotificationEnabled(ReadSettings().JsNotificationEnabled);
        services.SetNotificationEmitter(EmitScriptNotification);
    }

    public object Get()
    {
        lock (_lock)
            return Describe(ReadSettings());
    }

    public object Save(JObject settings)
    {
        var jsEnabled = settings.Value<bool?>("jsNotificationEnabled")
            ?? throw new ArgumentException("jsNotificationEnabled is required.");
        var nativeEnabled = settings.Value<bool?>("macOSNotificationEnabled")
            ?? throw new ArgumentException("macOSNotificationEnabled is required.");
        lock (_lock)
        {
            var root = LoadRoot();
            var notification = root["notificationConfig"] as JsonObject ?? [];
            notification["jsNotificationEnabled"] = jsEnabled;
            notification["windowsUwpNotificationEnabled"] = nativeEnabled;
            root["notificationConfig"] = notification;
            SaveRoot(root);
            _scriptHostServices?.SetJsNotificationEnabled(jsEnabled);
            return Describe(new Settings(jsEnabled, nativeEnabled));
        }
    }

    public object Test()
    {
        var settings = ReadSettings();
        if (!settings.MacOSNotificationEnabled)
            throw new InvalidOperationException("macOS 通知尚未启用。");
        Emit("info", "这是一条 BetterGI 测试通知。");
        return new { sent = true };
    }

    private void EmitScriptNotification(ScriptNotificationKind kind, string message)
    {
        if (!ReadSettings().MacOSNotificationEnabled)
            return;
        Emit(kind.ToString().ToLowerInvariant(), message);
    }

    private void Emit(string kind, string message)
    {
        var response = callbacks.InvokeAsync(
                "notification.emit",
                JObject.FromObject(new { kind, message }),
                sessionToken,
                cancellationToken)
            .GetAwaiter().GetResult();
        if (response?.Value<bool?>("acknowledged") != true)
            throw new InvalidDataException("notification.emit did not return acknowledged=true.");
    }

    private Settings ReadSettings()
    {
        lock (_lock)
        {
            var notification = LoadRoot()["notificationConfig"] as JsonObject;
            return new Settings(
                notification?["jsNotificationEnabled"]?.GetValue<bool>() ?? false,
                notification?["windowsUwpNotificationEnabled"]?.GetValue<bool>() ?? false);
        }
    }

    private object Describe(Settings settings) => new
    {
        jsNotificationEnabled = settings.JsNotificationEnabled,
        macOSNotificationEnabled = settings.MacOSNotificationEnabled
    };

    private JsonObject LoadRoot()
    {
        var path = Path.Combine(layout.UserPath, "config.json");
        if (!File.Exists(path))
            return [];
        return JsonNode.Parse(File.ReadAllText(path), documentOptions: new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip,
        }) as JsonObject ?? throw new InvalidDataException("User/config.json root must be an object.");
    }

    private void SaveRoot(JsonObject root)
    {
        Directory.CreateDirectory(layout.UserPath);
        var path = Path.Combine(layout.UserPath, "config.json");
        var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
        File.WriteAllText(temporaryPath, root.ToJsonString(ConfigJson.Options));
        File.Move(temporaryPath, path, true);
    }

    private sealed record Settings(
        bool JsNotificationEnabled,
        bool MacOSNotificationEnabled);
}
