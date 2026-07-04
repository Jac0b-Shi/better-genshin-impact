using System.Text.Json;

namespace BetterGenshinImpact.Service;

/// <summary>
/// TEMPORARY VERIFICATION SHIM: provides ConfigService.JsonOptions for JSON deserialization.
/// The real ConfigService manages file I/O and WPF configuration.
/// </summary>
public static class ConfigService
{
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };
}
