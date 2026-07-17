using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.Core.Host.Runtime;
using BetterGenshinImpact.Core.Script.Group;
using BetterGenshinImpact.Core.Script.Project;
using BetterGenshinImpact.Core.Script;
using Newtonsoft.Json.Linq;

var root = args.Length == 1
    ? Path.GetFullPath(args[0])
    : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "betterGI-mac");
var layout = new RuntimeLayout(root);
var groupPath = Path.Combine(layout.ScriptGroupPath, "狗粮+锄地.json");
if (!File.Exists(groupPath))
    throw new FileNotFoundException("Required real ScriptGroup is missing.", groupPath);

Global.StartUpPath = root;
var document = new ScriptGroupCatalog(layout).Get("狗粮+锄地");
var group = ScriptGroup.FromJson(document.Document.ToString());
if (group.Projects.Count == 0)
    throw new InvalidDataException("狗粮+锄地 contains no projects.");

var projectCatalog = new ScriptProjectCatalog(layout);
var javascriptProjects = group.Projects.Where(project => project.Type == "Javascript").ToArray();
if (javascriptProjects.Length == 0)
    throw new InvalidDataException("狗粮+锄地 contains no JavaScript projects.");

foreach (var groupProject in javascriptProjects)
{
    if (groupProject.JsScriptSettingsObject is null)
        throw new InvalidDataException($"{groupProject.FolderName} has no jsScriptSettingsObject.");
    var catalogProject = projectCatalog.Get(groupProject.FolderName);
    var scriptProject = new ScriptProject(groupProject.FolderName);
    if (!string.Equals(scriptProject.Manifest.Name, groupProject.Name, StringComparison.Ordinal))
        throw new InvalidDataException(
            $"Manifest name mismatch for {groupProject.FolderName}: {scriptProject.Manifest.Name} != {groupProject.Name}");
    _ = new PackageDocumentLoader(scriptProject.ProjectPath);
    Console.WriteLine(
        $"PASS {groupProject.FolderName}: main={scriptProject.Manifest.Main}, settings={((JArray)catalogProject.Settings).Count}, " +
        $"saved_files={scriptProject.Manifest.SavedFiles?.Length ?? 0}, libraries={scriptProject.Manifest.Library?.Length ?? 0}");
}

Console.WriteLine($"Real User verification passed: group={group.Name}, projects={group.Projects.Count}, javascript={javascriptProjects.Length}.");
