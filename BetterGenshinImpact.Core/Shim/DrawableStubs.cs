using OpenCvSharp;
using System.Collections.Generic;

namespace BetterGenshinImpact.View.Drawable;

/// <summary>
/// Temporary stub types for WPF overlay drawing consumed by linked Region.cs / ImageRegion.cs.
/// These are visual debugging/overlay features — no-ops in the cross-platform Core.
/// Cannot be deleted until drawing guards (#if BGI_FULL_WINDOWS) or upstream changes.
/// </summary>

public class RectDrawable
{
    public string Name { get; set; } = "";
}

public class LineDrawable
{
    public string Name { get; set; } = "";
}

public class DrawContent
{
    public List<object>? List { get; set; }
    public void PutRect(string name, RectDrawable rect) { }
    public void PutOrRemoveRectList(string name, List<RectDrawable> rects) { }
    public void RemoveRect(string name) { }
    public void PutLine(string name, LineDrawable line) { }
}

public class DrawableRect
{
    public DrawableRect(Rect rect, string text) { }
}

public class VisionContext
{
    private static VisionContext? _instance;
    public static VisionContext Instance() => _instance ??= new VisionContext();
    public DrawContent DrawContent { get; } = new();
}
