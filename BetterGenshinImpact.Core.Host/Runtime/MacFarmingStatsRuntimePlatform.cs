using System.Text.Json;
using System.Text.Json.Nodes;
using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.Core.Script.Dependence;
using BetterGenshinImpact.GameTask.FarmingPlan;
using Microsoft.Extensions.Logging;

namespace BetterGenshinImpact.Core.Host.Runtime;

public sealed class MacFarmingStatsRuntimePlatform(
    RuntimeLayout layout,
    ILogger logger) : IFarmingStatsRuntimePlatform
{
    public string LogDirectory { get; } = Path.Combine(layout.RootPath, "log", "FarmingPlan");
    public OtherConfig.FarmingPlan Config { get; } = LoadConfig(layout);
    public ILogger Logger { get; } = logger;
    public DateTimeOffset ServerTimeNow => ScriptHostServices.ServerTimeNow;

    public Task UpdateMiyousheDataAsync(CancellationToken cancellationToken) =>
        throw new CapabilityUnavailableException(
            "Farming-plan Miyoushe synchronization is not composed on macOS yet.");

    private static OtherConfig.FarmingPlan LoadConfig(RuntimeLayout layout)
    {
        var path = Path.Combine(layout.UserPath, "config.json");
        if (!File.Exists(path)) return new OtherConfig.FarmingPlan();
        var root = JsonNode.Parse(File.ReadAllText(path), documentOptions: new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        }) as JsonObject ?? throw new InvalidDataException("User/config.json root must be an object.");
        return root["otherConfig"]?["farmingPlanConfig"]
            ?.Deserialize<OtherConfig.FarmingPlan>(ConfigJson.Options)
            ?? new OtherConfig.FarmingPlan();
    }
}
