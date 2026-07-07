using OpenCvSharp;

namespace BetterGenshinImpact.GameTask.Common.BgiVision;

public static class Bv
{
    public static Mat ImRead(string path, ImreadModes mode = ImreadModes.Color) => Cv2.ImRead(path, mode);
}
