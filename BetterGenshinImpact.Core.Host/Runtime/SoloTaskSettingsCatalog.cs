using System.Text.Json;
using System.Text.Json.Nodes;
using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.GameTask.AutoCook;
using Newtonsoft.Json.Linq;

namespace BetterGenshinImpact.Core.Host.Runtime;

public sealed class SoloTaskSettingsCatalog(RuntimeLayout layout)
{
    private readonly object _lock = new();

    public object Get(string name)
    {
        if (name != "AutoCook")
            throw new CapabilityUnavailableException(
                $"solo task settings '{name}' are not composed in the macOS Core yet.");

        lock (_lock)
        {
            var config = LoadRoot()["autoCookConfig"]?.Deserialize<AutoCookConfig>(ConfigJson.Options)
                         ?? new AutoCookConfig();
            return Describe(config);
        }
    }

    public object Save(string name, JObject settings)
    {
        if (name != "AutoCook")
            throw new CapabilityUnavailableException(
                $"solo task settings '{name}' are not composed in the macOS Core yet.");

        var interval = settings.Value<int?>("checkIntervalMs")
                       ?? throw new ArgumentException("checkIntervalMs is required.");
        if (interval is < 1 or > 1000)
            throw new ArgumentOutOfRangeException(
                "checkIntervalMs", interval, "checkIntervalMs must be between 1 and 1000.");
        var stopWhenDetected = settings.Value<bool?>("stopTaskWhenRecoverButtonDetected")
                               ?? throw new ArgumentException(
                                   "stopTaskWhenRecoverButtonDetected is required.");

        lock (_lock)
        {
            var root = LoadRoot();
            var config = new AutoCookConfig
            {
                CheckIntervalMs = interval,
                StopTaskWhenRecoverButtonDetected = stopWhenDetected,
            };
            root["autoCookConfig"] = JsonSerializer.SerializeToNode(config, ConfigJson.Options);
            SaveRoot(root);
            return Describe(config);
        }
    }

    private static object Describe(AutoCookConfig config) => new
    {
        name = "AutoCook",
        checkIntervalMs = config.CheckIntervalMs,
        stopTaskWhenRecoverButtonDetected = config.StopTaskWhenRecoverButtonDetected,
    };

    private JsonObject LoadRoot()
    {
        var path = Path.Combine(layout.UserPath, "config.json");
        if (!File.Exists(path)) return [];
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
        var temporaryPath = path + ".tmp";
        File.WriteAllText(temporaryPath, root.ToJsonString(ConfigJson.Options));
        File.Move(temporaryPath, path, true);
    }
}
