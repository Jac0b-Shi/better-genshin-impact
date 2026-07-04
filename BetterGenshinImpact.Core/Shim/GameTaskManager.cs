using OpenCvSharp;

namespace BetterGenshinImpact.GameTask;

/// <summary>
/// Thin facade: PNG asset loader. Replaces upstream GameTaskManager.LoadAssetImage()
/// which depends on dozens of task-specific asset types (AutoBoss, AutoFight, etc.).
/// </summary>
public static class GameTaskManager
{
    /// <summary>
    /// Load a PNG asset image from the Assets directory.
    /// Path is resolved relative to the task's Assets/1920x1080/ subfolder.
    /// </summary>
    public static Mat LoadAssetImage(string taskName, string fileName)
    {
        // Resolve relative to the BetterGenshinImpact project root (where assets live)
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        // Walk up to find the BetterGenshinImpact directory
        var dir = baseDir;
        while (dir != null && !Directory.Exists(Path.Combine(dir, "BetterGenshinImpact")))
        {
            dir = Path.GetDirectoryName(dir);
        }

        var assetDir = dir != null
            ? Path.Combine(dir, "BetterGenshinImpact", "GameTask", taskName, "Assets", "1920x1080")
            : Path.Combine(baseDir, "GameTask", taskName, "Assets", "1920x1080");

        var filePath = Path.Combine(assetDir, fileName);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Asset not found: {filePath}");
        }

        return Cv2.ImRead(filePath, ImreadModes.Color);
    }
}
