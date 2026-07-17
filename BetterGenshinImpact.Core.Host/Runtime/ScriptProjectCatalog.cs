using BetterGenshinImpact.Core.Host.Protocol;
using BetterGenshinImpact.Core.Script.Project;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BetterGenshinImpact.Core.Host.Runtime;

public sealed class ScriptProjectCatalog(RuntimeLayout layout)
{
    private string Root => Path.Combine(layout.RootPath, "User", "JsScript");

    public IReadOnlyList<ScriptProjectDocument> List()
    {
        layout.EnsureCreated();
        if (!Directory.Exists(Root)) return [];
        return Directory.EnumerateDirectories(Root, "*", SearchOption.TopDirectoryOnly)
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .Select(path => Read(Path.GetFileName(path)))
            .ToArray();
    }

    public ScriptProjectDocument Get(string folderName) => Read(ValidateFolderName(folderName));

    private ScriptProjectDocument Read(string folderName)
    {
        var directory = Path.Combine(Root, ValidateFolderName(folderName));
        var manifestPath = Path.Combine(directory, "manifest.json");
        if (!File.Exists(manifestPath))
            throw new FileNotFoundException($"manifest.json does not exist for script project: {folderName}", manifestPath);
        var manifest = Manifest.FromJson(File.ReadAllText(manifestPath));
        manifest.Validate(directory);
        var jsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
        };
        return new ScriptProjectDocument(
            folderName,
            JObject.Parse(JsonConvert.SerializeObject(manifest, jsonSettings)),
            JArray.Parse(JsonConvert.SerializeObject(manifest.LoadSettingItems(directory), jsonSettings)));
    }

    private static string ValidateFolderName(string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName) || folderName is "." or ".." ||
            folderName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            folderName.Contains('/') || folderName.Contains('\\'))
            throw new ArgumentException("Script project folder name is invalid.", nameof(folderName));
        return folderName;
    }
}
