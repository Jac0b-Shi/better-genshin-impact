using BetterGenshinImpact.Core.Host.Transport;
using BetterGenshinImpact.GameTask.AutoPathing;
using BetterGenshinImpact.GameTask.AutoPathing.Model;
using Newtonsoft.Json.Linq;

namespace BetterGenshinImpact.Core.Host.Runtime;

public sealed class MacPathExecutorPlatform(
    PlatformCallbackChannel callbacks,
    string sessionToken,
    CancellationToken cancellationToken) : IPathExecutorPlatform
{
    private string _autoFetchDispatchAdventurersGuildCountry = "无";
    public (int Width, int Height) GetGameScreenSize()
    {
        var metrics = Invoke("window.metrics", null);
        return (
            RequiredInt(metrics, "width"),
            RequiredInt(metrics, "height"));
    }

    public void PublishCurrentPathing(PathingTask task)
    {
        var response = Invoke("pathing.current", JObject.FromObject(new
        {
            name = task.Info.Name,
            author = task.Info.Author,
            waypointCount = task.Positions.Count
        }));
        if (response.Value<bool?>("acknowledged") != true)
            throw new InvalidDataException("pathing.current did not return acknowledged=true.");
    }

    public string AutoFetchDispatchAdventurersGuildCountry =>
        Volatile.Read(ref _autoFetchDispatchAdventurersGuildCountry);

    public void SetAutoFetchDispatchAdventurersGuildCountry(string country)
    {
        if (string.IsNullOrWhiteSpace(country))
            throw new ArgumentException("Auto-fetch dispatch country cannot be empty.", nameof(country));
        Volatile.Write(ref _autoFetchDispatchAdventurersGuildCountry, country);
    }

    private JObject Invoke(string method, JObject? parameters) =>
        callbacks.InvokeAsync(method, parameters, sessionToken, cancellationToken)
            .GetAwaiter().GetResult() as JObject
        ?? throw new InvalidDataException($"{method} did not return an object.");

    private static int RequiredInt(JObject value, string name) =>
        value.Value<int?>(name)
        ?? throw new InvalidDataException($"window.metrics did not return {name}.");
}
