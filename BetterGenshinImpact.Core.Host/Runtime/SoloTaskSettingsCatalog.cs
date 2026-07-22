using System.Text.Json;
using System.Text.Json.Nodes;
using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.GameTask.AutoBoss;
using BetterGenshinImpact.GameTask.AutoCook;
using BetterGenshinImpact.GameTask.AutoMusicGame;
using BetterGenshinImpact.GameTask.AutoWood;
using Newtonsoft.Json.Linq;

namespace BetterGenshinImpact.Core.Host.Runtime;

public sealed class SoloTaskSettingsCatalog(RuntimeLayout layout)
{
    private readonly object _lock = new();
    public int AutoWoodRoundNum { get; private set; }
    public int AutoWoodDailyMaxCount { get; private set; } = 2000;

    public object Get(string name)
    {
        lock (_lock)
        {
            var root = LoadRoot();
            return name switch
            {
                "AutoCook" => Describe(LoadConfig<AutoCookConfig>(root, "autoCookConfig")),
                "AutoWood" => Describe(LoadConfig<AutoWoodConfig>(root, "autoWoodConfig")),
                "AutoMusicGame" => Describe(
                    LoadConfig<AutoMusicGameConfig>(root, "autoMusicGameConfig")),
                "AutoBoss" => Describe(LoadConfig<AutoBossConfig>(root, "autoBossConfig")),
                _ => throw Unavailable(name),
            };
        }
    }

    public object Save(string name, JObject settings)
    {
        return name switch
        {
            "AutoCook" => SaveAutoCook(settings),
            "AutoWood" => SaveAutoWood(settings),
            "AutoMusicGame" => SaveAutoMusicGame(settings),
            "AutoBoss" => SaveAutoBoss(settings),
            _ => throw Unavailable(name),
        };
    }

    private object SaveAutoCook(JObject settings)
    {
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

    private object SaveAutoWood(JObject settings)
    {
        var roundNum = RequiredInt(settings, "roundNum");
        var dailyMaxCount = RequiredInt(settings, "dailyMaxCount");
        var config = new AutoWoodConfig
        {
            UseWonderlandRefresh = RequiredBool(settings, "useWonderlandRefresh"),
            WoodCountOcrEnabled = RequiredBool(settings, "woodCountOcrEnabled"),
            AfterZSleepDelay = RequiredInt(settings, "afterZSleepDelay"),
        };
        lock (_lock)
        {
            AutoWoodRoundNum = roundNum;
            AutoWoodDailyMaxCount = dailyMaxCount;
            SaveConfig("autoWoodConfig", config);
            return Describe(config);
        }
    }

    private object SaveAutoMusicGame(JObject settings)
    {
        var musicLevel = RequiredString(settings, "musicLevel");
        if (!MusicLevels.Contains(musicLevel, StringComparer.Ordinal))
            throw new ArgumentException($"Unsupported musicLevel: {musicLevel}");
        var config = new AutoMusicGameConfig
        {
            MustCanorusLevel = RequiredBool(settings, "mustCanorusLevel"),
            MusicLevel = musicLevel,
        };
        lock (_lock)
        {
            SaveConfig("autoMusicGameConfig", config);
            return Describe(config);
        }
    }

    private object SaveAutoBoss(JObject settings)
    {
        var bossName = RequiredString(settings, "bossName");
        if (!string.IsNullOrEmpty(bossName) && !AutoBossData.IsSupportedBoss(bossName))
            throw new ArgumentException($"Unsupported bossName: {bossName}");
        var strategyName = RequiredString(settings, "strategyName");
        if (!StrategyOptions().Contains(strategyName, StringComparer.Ordinal))
            throw new ArgumentException($"Unknown AutoFight strategy: {strategyName}");
        var config = new AutoBossConfig
        {
            BossName = bossName,
            StrategyName = strategyName,
            TeamName = RequiredString(settings, "teamName"),
            SpecifyRunCount = RequiredBool(settings, "specifyRunCount"),
            RunCount = Math.Max(1, RequiredInt(settings, "runCount")),
            UseTransientResin = RequiredBool(settings, "useTransientResin"),
            UseFragileResin = RequiredBool(settings, "useFragileResin"),
            ReturnToStatueAfterEachRound = RequiredBool(
                settings, "returnToStatueAfterEachRound"),
            RewardRecognitionEnabled = RequiredBool(settings, "rewardRecognitionEnabled"),
            ReviveRetryCount = Math.Max(0, RequiredInt(settings, "reviveRetryCount")),
        };
        if (!config.SpecifyRunCount)
        {
            config.UseTransientResin = false;
            config.UseFragileResin = false;
        }
        lock (_lock)
        {
            SaveConfig("autoBossConfig", config);
            return Describe(config);
        }
    }

    private static object Describe(AutoCookConfig config) => new
    {
        name = "AutoCook",
        checkIntervalMs = config.CheckIntervalMs,
        stopTaskWhenRecoverButtonDetected = config.StopTaskWhenRecoverButtonDetected,
    };

    private object Describe(AutoWoodConfig config) => new
    {
        name = "AutoWood",
        roundNum = AutoWoodRoundNum,
        dailyMaxCount = AutoWoodDailyMaxCount,
        useWonderlandRefresh = config.UseWonderlandRefresh,
        woodCountOcrEnabled = config.WoodCountOcrEnabled,
        afterZSleepDelay = config.AfterZSleepDelay,
    };

    private static readonly string[] MusicLevels = ["传说", "大师", "困难", "普通", "所有"];

    private static object Describe(AutoMusicGameConfig config) => new
    {
        name = "AutoMusicGame",
        mustCanorusLevel = config.MustCanorusLevel,
        musicLevel = string.IsNullOrEmpty(config.MusicLevel) ? MusicLevels[0] : config.MusicLevel,
        musicLevelOptions = MusicLevels,
    };

    private object Describe(AutoBossConfig config) => new
    {
        name = "AutoBoss",
        bossName = config.BossName,
        bossOptions = AutoBossData.SupportedBossNames,
        strategyName = config.StrategyName,
        strategyOptions = StrategyOptions(),
        teamName = config.TeamName,
        specifyRunCount = config.SpecifyRunCount,
        runCount = config.RunCount,
        useTransientResin = config.UseTransientResin,
        useFragileResin = config.UseFragileResin,
        returnToStatueAfterEachRound = config.ReturnToStatueAfterEachRound,
        rewardRecognitionEnabled = config.RewardRecognitionEnabled,
        reviveRetryCount = config.ReviveRetryCount,
    };

    private string[] StrategyOptions()
    {
        var folder = Path.Combine(layout.UserPath, "AutoFight");
        Directory.CreateDirectory(folder);
        return
        [
            "根据队伍自动选择",
            .. Directory.EnumerateFiles(folder, "*.*", SearchOption.AllDirectories)
                .Where(path => path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
                               path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                .Select(path => Path.ChangeExtension(Path.GetRelativePath(folder, path), null))
                .Order(StringComparer.Ordinal),
        ];
    }

    private static T LoadConfig<T>(JsonObject root, string propertyName) where T : class, new() =>
        root[propertyName]?.Deserialize<T>(ConfigJson.Options) ?? new T();

    private void SaveConfig<T>(string propertyName, T config)
    {
        var root = LoadRoot();
        root[propertyName] = JsonSerializer.SerializeToNode(config, ConfigJson.Options);
        SaveRoot(root);
    }

    private static int RequiredInt(JObject settings, string name) =>
        settings.Value<int?>(name) ?? throw new ArgumentException($"{name} is required.");

    private static bool RequiredBool(JObject settings, string name) =>
        settings.Value<bool?>(name) ?? throw new ArgumentException($"{name} is required.");

    private static string RequiredString(JObject settings, string name) =>
        settings.Value<string>(name) ?? throw new ArgumentException($"{name} is required.");

    private static CapabilityUnavailableException Unavailable(string name) => new(
        $"solo task settings '{name}' are not composed in the macOS Core yet.");

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
