using BetterGenshinImpact.Core.Recognition;
using BetterGenshinImpact.Core.Recognition.OpenCv;
using BetterGenshinImpact.GameTask.Model;
using OpenCvSharp;

namespace BetterGenshinImpact.Core.Host.Runtime;

/// <summary>The exact two upstream recognition objects required by Bv.IsInMainUi.</summary>
public sealed class MacMainUiRecognitionAssets : IDisposable
{
    public MacMainUiRecognitionAssets(RuntimeLayout layout, ISystemInfo systemInfo)
    {
        var captureRect = systemInfo.ScaleMax1080PCaptureRect;
        PaimonMenu = new RecognitionObject
        {
            Name = "PaimonMenu",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = Load(layout, systemInfo, "Common/Element", "paimon_menu.png"),
            RegionOfInterest = new Rect(0, 0, captureRect.Width / 4, captureRect.Height / 4),
            DrawOnWindow = false
        }.InitTemplate();
        ReviveConfirm = new RecognitionObject
        {
            Name = "Confirm",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = Load(layout, systemInfo, "AutoFight", "confirm.png"),
            RegionOfInterest = new Rect(
                captureRect.Width / 2, captureRect.Height / 2,
                captureRect.Width / 2, captureRect.Height / 2),
            DrawOnWindow = false
        }.InitTemplate();
        GirlMoon = Template(layout, systemInfo, "GameLoading", "girl_moon.png",
            new Rect(0, captureRect.Height / 2, captureRect.Width, captureRect.Height / 2), "GirlMoon");
        WelkinMoon = Template(layout, systemInfo, "GameLoading", "welkin_moon_logo.png",
            new Rect(0, captureRect.Height / 2, captureRect.Width, captureRect.Height / 2), "WelkinMoon");
        Primogem = Template(layout, systemInfo, "Common/Element", "primogem.png",
            new Rect(0, captureRect.Height / 3, captureRect.Width, captureRect.Height / 3), "Primogem");
    }

    public RecognitionObject PaimonMenu { get; }
    public RecognitionObject ReviveConfirm { get; }
    public RecognitionObject GirlMoon { get; }
    public RecognitionObject WelkinMoon { get; }
    public RecognitionObject Primogem { get; }

    public void Dispose()
    {
        Dispose(PaimonMenu);
        Dispose(ReviveConfirm);
        Dispose(GirlMoon);
        Dispose(WelkinMoon);
        Dispose(Primogem);
    }

    private static Mat Load(RuntimeLayout layout, ISystemInfo systemInfo, string feature, string name)
    {
        var resolution = $"{systemInfo.GameScreenSize.Width}x{systemInfo.GameScreenSize.Height}";
        var directory = Path.Combine(layout.RootPath, "Assets", "GameTask", feature, "Assets", resolution);
        if (!Directory.Exists(directory))
            directory = Path.Combine(layout.RootPath, "Assets", "GameTask", feature, "Assets", "1920x1080");
        var path = Path.Combine(directory, name);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Missing BetterGI recognition asset: {feature}/{name}", path);
        using var stream = File.OpenRead(path);
        var source = Mat.FromStream(stream, ImreadModes.Color);
        if (Math.Abs(systemInfo.AssetScale - 1d) < 0.00001)
            return source;
        try
        {
            return ResizeHelper.Resize(source, systemInfo.AssetScale);
        }
        finally
        {
            source.Dispose();
        }
    }

    private static RecognitionObject Template(
        RuntimeLayout layout, ISystemInfo systemInfo, string feature, string name, Rect roi, string objectName) =>
        new RecognitionObject
        {
            Name = objectName,
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = Load(layout, systemInfo, feature, name),
            RegionOfInterest = roi,
            DrawOnWindow = false
        }.InitTemplate();

    private static void Dispose(RecognitionObject recognitionObject)
    {
        recognitionObject.MaskMat?.Dispose();
        recognitionObject.TemplateImageGreyMat?.Dispose();
        recognitionObject.TemplateImageMat?.Dispose();
    }
}
