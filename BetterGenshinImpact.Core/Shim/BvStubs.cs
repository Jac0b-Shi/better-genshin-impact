using BetterGenshinImpact.GameTask.Model.Area;
using OpenCvSharp;

namespace BetterGenshinImpact.GameTask.Common.BgiVision;

public static class Bv
{
    public static GameUiCategory WhichGameUi(ImageRegion region) => GameUiCategory.Unknown;
    public static GameUiCategory WhichGameUi() => GameUiCategory.Unknown;
    public static GameUiCategory WhichGameUiForTriggers(ImageRegion region) => GameUiCategory.Unknown;
    public static bool DetectChatUi(ImageRegion region) => false;
    public static Mat ImRead(string path, ImreadModes mode = ImreadModes.Color) => Cv2.ImRead(path, mode);
}
