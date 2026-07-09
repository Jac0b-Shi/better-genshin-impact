using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BetterGenshinImpact.Core.Artifacts;

public class ModelArtifactManifest
{
    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("modelRootContract")]
    public string ModelRootContract { get; set; } = "";

    [JsonPropertyName("artifacts")]
    public List<ModelArtifactEntry> Artifacts { get; set; } = [];

    [JsonPropertyName("sidecarArtifacts")]
    public List<SidecarArtifactEntry> SidecarArtifacts { get; set; } = [];
}

public class ModelArtifactEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "";

    [JsonPropertyName("relativePath")]
    public string RelativePath { get; set; } = "";

    [JsonPropertyName("registryKey")]
    public string RegistryKey { get; set; } = "";

    [JsonPropertyName("requiredFor")]
    public List<string> RequiredFor { get; set; } = [];

    [JsonPropertyName("sidecars")]
    public List<string> Sidecars { get; set; } = [];
}

public class SidecarArtifactEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("relativePath")]
    public string RelativePath { get; set; } = "";

    [JsonPropertyName("requiredFor")]
    public List<string> RequiredFor { get; set; } = [];
}
