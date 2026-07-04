using OpenCvSharp;
using System.Drawing;

namespace BetterGenshinImpact.Core.Recognition.OpenCv;

public static class CoreOpenCvExtensions
{
    public static Scalar ToScalar(this Color color) =>
        new(color.B, color.G, color.R);

    public static Rect ClampTo(this Rect rect, Rect bounds)
    {
        var x = Math.Max(rect.X, bounds.X);
        var y = Math.Max(rect.Y, bounds.Y);
        var r = Math.Min(rect.Right, bounds.Right);
        var b = Math.Min(rect.Bottom, bounds.Bottom);
        return r > x && b > y ? Rect.FromLTRB(x, y, r, b) : new Rect();
    }

    public static Rect ClampTo(this Rect rect, Mat bounds) =>
        rect.ClampTo(new Rect(0, 0, bounds.Width, bounds.Height));
}
