using BetterGenshinImpact.Core.Config;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BetterGenshinImpact.Core.Host.Runtime;

public sealed class MacroSettingsCatalog(RuntimeLayout layout)
{
    private readonly object _lock = new();

    public object Get()
    {
        lock (_lock)
            return Describe(ReadSettings(LoadRoot()));
    }

    public object Save(JObject settings)
    {
        lock (_lock)
        {
            var root = LoadRoot();
            var current = ReadSettings(root);
            var next = new Settings(
                RequiredBoolean(settings, "fPressHoldToContinuationEnabled"),
                RequiredInterval(settings, "fFireInterval"),
                RequiredBoolean(settings, "spacePressHoldToContinuationEnabled"),
                RequiredInterval(settings, "spaceFireInterval"),
                current.PickUpOrInteractKeyCode,
                current.JumpKeyCode);
            var macro = root["macroConfig"] as JsonObject ?? [];
            macro["fPressHoldToContinuationEnabled"] = next.FEnabled;
            macro["fFireInterval"] = next.FInterval;
            macro["spacePressHoldToContinuationEnabled"] = next.SpaceEnabled;
            macro["spaceFireInterval"] = next.SpaceInterval;
            root["macroConfig"] = macro;
            SaveRoot(root);
            return Describe(next);
        }
    }

    private static object Describe(Settings settings) => new
    {
        fPressHoldToContinuationEnabled = settings.FEnabled,
        fFireInterval = settings.FInterval,
        spacePressHoldToContinuationEnabled = settings.SpaceEnabled,
        spaceFireInterval = settings.SpaceInterval,
        pickUpOrInteractKeyCode = settings.PickUpOrInteractKeyCode,
        jumpKeyCode = settings.JumpKeyCode
    };

    private static Settings ReadSettings(JsonObject root)
    {
        var macro = root["macroConfig"] as JsonObject;
        var keyBindings = root["keyBindingsConfig"] as JsonObject;
        return new Settings(
            macro?["fPressHoldToContinuationEnabled"]?.GetValue<bool>() ?? false,
            macro?["fFireInterval"]?.GetValue<int>() ?? 100,
            macro?["spacePressHoldToContinuationEnabled"]?.GetValue<bool>() ?? false,
            macro?["spaceFireInterval"]?.GetValue<int>() ?? 100,
            keyBindings?["pickUpOrInteract"]?.GetValue<int>() ?? 0x46,
            keyBindings?["jump"]?.GetValue<int>() ?? 0x20);
    }

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

    private static bool RequiredBoolean(JObject settings, string name) =>
        settings.Value<bool?>(name) ?? throw new ArgumentException($"{name} is required.");

    private static int RequiredInterval(JObject settings, string name)
    {
        var value = settings.Value<int?>(name)
            ?? throw new ArgumentException($"{name} is required.");
        return value is >= 10 and <= 10_000
            ? value
            : throw new ArgumentOutOfRangeException(name, "Interval must be between 10 and 10000 ms.");
    }

    private sealed record Settings(
        bool FEnabled,
        int FInterval,
        bool SpaceEnabled,
        int SpaceInterval,
        int PickUpOrInteractKeyCode,
        int JumpKeyCode);
}
