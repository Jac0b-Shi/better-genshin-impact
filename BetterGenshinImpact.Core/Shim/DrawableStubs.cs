namespace BetterGenshinImpact.View.Drawable;

/// <summary>
/// Stub types for WPF overlay drawing (RectDrawable, LineDrawable, DrawContent).
/// These are visual debugging/overlay features, not business logic.
/// All methods are no-ops in the cross-platform Core — drawing only works on Windows/WPF host.
/// </summary>
public class RectDrawable
{
    public string Name { get; set; } = "";
}

public class LineDrawable
{
    public string Name { get; set; } = "";
}

/// <summary>
/// WPF overlay drawing context. Stub for cross-platform Core.
/// </summary>
public class DrawContent
{
    public void PutRect(string name, RectDrawable rect) { }
    public void PutOrRemoveRectList(string name, List<RectDrawable> rects) { }
    public void RemoveRect(string name) { }
    public void PutLine(string name, LineDrawable line) { }
}

/// <summary>
/// Stub for VisionContext which holds the DrawContent singleton.
/// </summary>
public class VisionContext
{
    private static VisionContext? _instance;
    public static VisionContext Instance() => _instance ??= new VisionContext();
    public DrawContent DrawContent { get; } = new();
}
