using BetterGenshinImpact.GameTask.Common.Element.Assets;
using BetterGenshinImpact.GameTask.Model.Area;

namespace BetterGenshinImpact.GameTask.Common.BgiVision;

public static partial class Bv
{
    public static MotionStatus GetMotionStatus(ImageRegion captureRa)
    {
        using var spaceRa = captureRa.Find(ElementAssets.Instance.SpaceKey);
        var spaceExist = spaceRa.IsExist();
        using var xRa = captureRa.Find(ElementAssets.Instance.XKey);
        var xExist = xRa.IsExist();
        if (spaceExist)
        {
            return xExist ? MotionStatus.Climb : MotionStatus.Fly;
        }

        return MotionStatus.Normal;
    }
}

public enum MotionStatus
{
    Normal,
    Fly,
    Climb,
}
