using System.Diagnostics;
using BetterGenshinImpact.GameTask.Model;
using BetterGenshinImpact.GameTask.Model.Area;
using BetterGenshinImpact.Platform.Abstractions;
using OpenCvSharp;

namespace BetterGenshinImpact.GameTask;

/// <summary>
/// TEMPORARY VERIFICATION SHIM: macOS ISystemInfo implementation.
/// Populated from IPlatformWindowService metrics for AutoPick closed-loop testing.
/// NOT a long-term architecture — the real solution splits upstream TaskContext to depend on
/// platform abstractions rather than maintaining a parallel implementation.
/// </summary>
public class MacSystemInfo : ISystemInfo
{
    public System.Drawing.Size DisplaySize { get; }
    public BgiRect GameScreenSize { get; }
    public double AssetScale { get; } = 1;
    public double ZoomOutMax1080PRatio { get; } = 1;
    public double ScaleTo1080PRatio { get; } = 1;
    public BgiRect CaptureAreaRect { get; set; }
    public BgiRect ScaleMax1080PCaptureRect { get; set; }
    public Process? GameProcess => null;
    public string GameProcessName { get; } = "GenshinImpact";
    public int GameProcessId { get; }
    public DesktopRegion DesktopRectArea { get; }

    public MacSystemInfo()
    {
        DisplaySize = new System.Drawing.Size(1920, 1080);
        GameScreenSize = new Rect(0, 0, 1920, 1080);
        CaptureAreaRect = new Rect(0, 0, 1920, 1080);
        ScaleMax1080PCaptureRect = new Rect(0, 0, 1920, 1080);
        DesktopRectArea = new DesktopRegion();
    }

    public MacSystemInfo(GameWindowMetrics m)
    {
        DisplaySize = new System.Drawing.Size(m.Width, m.Height);
        GameScreenSize = new Rect(0, 0, m.CaptureWidth, m.CaptureHeight);
        CaptureAreaRect = new Rect(m.CaptureX, m.CaptureY, m.CaptureWidth, m.CaptureHeight);
        ScaleMax1080PCaptureRect = new Rect(0, 0,
            Math.Min(m.CaptureWidth, 1920),
            (int)(Math.Min(m.CaptureHeight, 1080 * m.ScaleTo1080PRatio)));
        AssetScale = m.AssetScale;
        ScaleTo1080PRatio = m.ScaleTo1080PRatio;
        GameProcessId = m.ProcessId;
        DesktopRectArea = new DesktopRegion();
    }
}
