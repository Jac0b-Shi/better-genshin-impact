using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace BetterGenshinImpact.GameTask;

public static class TaskControl
{
    public static ILogger Logger { get; } = NullLogger.Instance;
    public static Model.Area.ImageRegion CaptureToRectArea() =>
        new(new OpenCvSharp.Mat(), 0, 0);
    public static void Sleep(int milliseconds) => Thread.Sleep(milliseconds);
}
