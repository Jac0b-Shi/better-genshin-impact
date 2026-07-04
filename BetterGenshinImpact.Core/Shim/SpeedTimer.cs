using System.Diagnostics;

namespace BetterGenshinImpact.Helpers;

/// <summary>
/// Performance timer for debug output. Linked from upstream (pure C#, no Windows deps).
/// </summary>
public class SpeedTimer
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly Dictionary<string, long> _records = new();

    public void Record(string name)
    {
        _records[name] = _stopwatch.ElapsedMilliseconds;
    }

    public void DebugPrint()
    {
        // No-op in release; debug-only in upstream
    }
}
