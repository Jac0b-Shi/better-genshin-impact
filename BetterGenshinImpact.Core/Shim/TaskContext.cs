using BetterGenshinImpact.Core.Config;
using BetterGenshinImpact.Core.Recognition;
using BetterGenshinImpact.GameTask.AutoPick;
using BetterGenshinImpact.GameTask.Model;
using BetterGenshinImpact.Platform.Abstractions;

namespace BetterGenshinImpact.GameTask;

/// <summary>
/// Thin facade: provides TaskContext.Instance() for cross-platform Core.
/// Windows-specific fields (GameHandle, PostMessageSimulator, WinEventHook) are excluded.
/// </summary>
public class TaskContext
{
    private static TaskContext? _instance;
    private static readonly object _locker = new();

    public static TaskContext Instance()
    {
        if (_instance == null)
        {
            lock (_locker)
            {
                _instance ??= new TaskContext();
            }
        }
        return _instance;
    }

    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Platform-agnostic system info. On macOS, populated from ScreenCaptureKit metrics.
    /// On Windows, populated from Win32.
    /// </summary>
    public ISystemInfo SystemInfo { get; set; } = new MacSystemInfo();

    /// <summary>
    /// Config access. For verification, this is a simple container.
    /// </summary>
    public CoreConfig Config { get; set; } = new();

    public void Init(GameWindowMetrics metrics)
    {
        SystemInfo = new MacSystemInfo(metrics);
        IsInitialized = true;
    }

    public static void DestroyInstance()
    {
        lock (_locker)
        {
            _instance = null;
        }
    }
}

/// <summary>
/// Minimal config container for AutoPick verification.
/// Does NOT include all upstream AllConfig fields - only what AutoPick needs.
/// </summary>
public class CoreConfig
{
    public AutoPickConfig AutoPickConfig { get; set; } = new();
    public OtherConfig OtherConfig { get; set; } = new();
}
