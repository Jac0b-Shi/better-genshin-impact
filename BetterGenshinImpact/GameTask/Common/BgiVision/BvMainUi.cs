using BetterGenshinImpact.Core.Recognition;
using BetterGenshinImpact.GameTask.Model.Area;
using OpenCvSharp;
using System.Linq;

namespace BetterGenshinImpact.GameTask.Common.BgiVision;

public static partial class Bv
{
    /// <summary>
    /// Shared main-UI recognition body. The Windows composition supplies the original
    /// ElementAssets/AutoFightAssets objects; the macOS composition builds the same
    /// recognition objects from its runtime asset root.
    /// </summary>
    public static bool IsInMainUi(
        ImageRegion captureRa,
        RecognitionObject paimonMenuRo,
        RecognitionObject reviveConfirmRo,
        string revivalText)
    {
        using var ra = captureRa.Find(paimonMenuRo);
        return ra.IsExist() && !IsInRevivePrompt(captureRa, reviveConfirmRo, revivalText);
    }

    internal static bool IsInRevivePrompt(
        ImageRegion region,
        RecognitionObject reviveConfirmRo,
        string revivalText)
    {
        using var confirmRectArea = region.Find(reviveConfirmRo);
        if (!confirmRectArea.IsEmpty())
        {
            var list = region.FindMulti(new RecognitionObject
            {
                RecognitionType = RecognitionTypes.Ocr,
                RegionOfInterest = new Rect(0, 0, region.Width, region.Height / 2)
            });

            if (list.Any(r => r.Text.Contains(revivalText)))
                return true;
        }

        return false;
    }

    public static bool IsInBlessingOfTheWelkinMoon(
        ImageRegion captureRa,
        RecognitionObject girlMoonRo,
        RecognitionObject welkinMoonRo)
    {
        using var girlRa = captureRa.Find(girlMoonRo);
        if (girlRa.IsExist())
            return true;
        using var moonRa = captureRa.Find(welkinMoonRo);
        return moonRa.IsExist();
    }
}
