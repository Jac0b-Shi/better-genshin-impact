using BetterGenshinImpact.GameTask.Model.Area.Converter;
using OpenCvSharp;

namespace BetterGenshinImpact.GameTask.Model.Area;

/// <summary>
/// 桌面区域类。
/// IInputBackend 使用屏幕像素坐标（非 0–65535 SendInput 体系）。
/// 0–65535 转换由 Windows InputBackend adapter 内部处理。
/// </summary>
public class DesktopRegion : Region
{
    public static int DisplayWidth { get; set; }
    public static int DisplayHeight { get; set; }

    public DesktopRegion(int w, int h) : base(0, 0, w, h) { }

    public DesktopRegion() : this(DisplayWidth, DisplayHeight)
    {
        if (DisplayWidth == 0 || DisplayHeight == 0)
            throw new InvalidOperationException(
                "DesktopRegion.DisplayWidth/Height must be initialized via platform window service before using the parameterless constructor.");
    }

    // --- Instance methods: coords are desktop-region-relative pixels ---

    public void DesktopRegionClick(int x, int y, int w, int h)
    {
        var input = PlatformServices.Input;
        var cx = x + w / 2;
        var cy = y + h / 2;
        input.MoveMouseTo(cx, cy);
        input.LeftButtonDown();
        Thread.Sleep(50);
        input.LeftButtonUp();
        Thread.Sleep(50);
    }

    public void DesktopRegionMove(int x, int y, int w, int h)
    {
        PlatformServices.Input.MoveMouseTo(x + w / 2, y + h / 2);
    }

    // --- Static methods: cx, cy are desktop screen pixels ---

    public static void DesktopRegionClick(double cx, double cy)
    {
        var input = PlatformServices.Input;
        input.MoveMouseTo((int)Math.Round(cx), (int)Math.Round(cy));
        input.LeftButtonDown();
        Thread.Sleep(50);
        input.LeftButtonUp();
        Thread.Sleep(50);
    }

    public static void DesktopRegionMove(double cx, double cy)
    {
        PlatformServices.Input.MoveMouseTo((int)Math.Round(cx), (int)Math.Round(cy));
    }

    public static void DesktopRegionMoveBy(double dx, double dy)
    {
        PlatformServices.Input.MoveMouseBy((int)dx, (int)dy);
    }

    public GameCaptureRegion Derive(Mat captureMat, int x, int y)
    {
        return new GameCaptureRegion(captureMat, x, y, this, new TranslationConverter(x, y));
    }
}
