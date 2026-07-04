namespace BetterGenshinImpact.GameTask;

/// <summary>
/// TEMPORARY VERIFICATION SHIM: provides RunnerContext.Instance.AutoPickTriggerStopCount.
/// The real RunnerContext references AutoFight, AutoPathing, CombatScenes, TaskProgress etc.
/// Long-term: split upstream RunnerContext into a Core-facing interface.
/// </summary>
public class RunnerContext
{
    private static RunnerContext? _instance;
    private static readonly object _locker = new();

    public static RunnerContext Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_locker)
                    _instance ??= new RunnerContext();
            }
            return _instance;
        }
    }

    /// <summary>
    /// Used by AutoPickTrigger to pause picking when stop count > 0.
    /// </summary>
    public volatile int AutoPickTriggerStopCount;
}
