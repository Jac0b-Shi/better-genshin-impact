using BetterGenshinImpact.GameTask.Model;
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
        return LoadAssetImage(taskName, fileName, null);
    }

    /// <summary>
    /// Load a PNG asset image, scaled for the given ISystemInfo resolution.
    /// When systemInfo is null, falls back to 1920x1080 assets.
    /// </summary>
    public static Mat LoadAssetImage(string taskName, string fileName, ISystemInfo? systemInfo)
    {
        var resolutionDir = (systemInfo?.GameScreenSize.Width).HasValue
            ? $"{systemInfo!.GameScreenSize.Width}x{systemInfo.GameScreenSize.Height}"
            : "1920x1080";

        // Resolve relative to the BetterGenshinImpact project root (where assets live)
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        // Walk up to find the BetterGenshinImpact directory
        var dir = baseDir;
        while (dir != null && !Directory.Exists(Path.Combine(dir, "BetterGenshinImpact")))
        {
            dir = Path.GetDirectoryName(dir);
        }

        var assetDir = dir != null
            ? Path.Combine(dir, "BetterGenshinImpact", "GameTask", taskName, "Assets", resolutionDir)
            : Path.Combine(baseDir, "GameTask", taskName, "Assets", resolutionDir);

        var filePath = Path.Combine(assetDir, fileName);
        if (!File.Exists(filePath))
        {
            // Fallback to 1920x1080
            var fallbackDir = dir != null
                ? Path.Combine(dir, "BetterGenshinImpact", "GameTask", taskName, "Assets", "1920x1080")
                : Path.Combine(baseDir, "GameTask", taskName, "Assets", "1920x1080");
            filePath = Path.Combine(fallbackDir, fileName);
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Asset not found: {filePath}");
            }
        }

        return Cv2.ImRead(filePath, ImreadModes.Color);
    }
}
