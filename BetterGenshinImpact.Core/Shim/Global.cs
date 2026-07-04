namespace BetterGenshinImpact.Core.Config;

/// <summary>
/// TEMPORARY VERIFICATION SHIM: file I/O and path helpers.
/// The real Global.cs depends on Semver and app-assembly metadata.
/// Long-term: split upstream Global into Core-safe and host-specific parts.
/// </summary>
public static class Global
{
    public static string Version { get; } = "0.0.0-mac-core";
    public static string StartUpPath { get; set; } = AppDomain.CurrentDomain.BaseDirectory;

    /// <summary>Resolve a relative path to an absolute path under the project root.</summary>
    public static string Absolute(string relativePath)
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var dir = baseDir;
        while (dir != null && !Directory.Exists(Path.Combine(dir, "BetterGenshinImpact")))
            dir = Path.GetDirectoryName(dir);

        var root = dir ?? baseDir;
        return Path.Combine(root, relativePath);
    }

    public static string? ReadAllTextIfExist(string relativePath)
    {
        var fullPath = Absolute(relativePath);
        return File.Exists(fullPath) ? File.ReadAllText(fullPath) : null;
    }

    public static void WriteAllText(string relativePath, string content)
    {
        var fullPath = Absolute(relativePath);
        var dir = Path.GetDirectoryName(fullPath);
        if (dir != null) Directory.CreateDirectory(dir);
        File.WriteAllText(fullPath, content);
    }
}
