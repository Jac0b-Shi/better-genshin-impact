using BetterGenshinImpact.Core.Recognition;
using BetterGenshinImpact.GameTask.Model;
using OpenCvSharp;

namespace BetterGenshinImpact.GameTask.AutoWood.Assets;

public class AutoWoodAssets : BaseAssets<AutoWoodAssets>
{
    public RecognitionObject TheBoonOfTheElderTreeRo = null!;

    // public RecognitionObject CharacterGuideRo;
    public RecognitionObject MenuBagRo = null!;

    public RecognitionObject ConfirmRo = null!;
    public RecognitionObject EnterGameRo = null!;

    // 木头数量
    public Rect WoodCountUpperRect;

#if BGI_FULL_WINDOWS
    private AutoWoodAssets() : base()
    {
        Initialization(systemInfo);
    }
#else
    public static void Initialize(ISystemInfo systemInfo)
    {
        ArgumentNullException.ThrowIfNull(systemInfo);
        if (_instance is not null)
            throw new InvalidOperationException("AutoWoodAssets is already initialized. Call DestroyInstance() first.");
        _instance = new AutoWoodAssets(systemInfo);
    }

    public new static AutoWoodAssets Instance => _instance
        ?? throw new InvalidOperationException("AutoWoodAssets.Initialize(...) must be called before Instance.");

    private AutoWoodAssets(ISystemInfo systemInfo) : base(systemInfo)
    {
        Initialization(systemInfo);
    }
#endif

    private void Initialization(ISystemInfo systemInfo)
    {

        WoodCountUpperRect = new Rect((int)(100 * AssetScale), (int)(450 * AssetScale), (int)(300 * AssetScale), (int)(250 * AssetScale));

        //「王树瑞佑」
        TheBoonOfTheElderTreeRo = new RecognitionObject
        {
            Name = "TheBoonOfTheElderTree",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("AutoWood", "TheBoonOfTheElderTree.png", systemInfo),
            RegionOfInterest = new Rect(CaptureRect.Width - CaptureRect.Width / 4, CaptureRect.Height / 2,
                CaptureRect.Width / 4, CaptureRect.Height - CaptureRect.Height / 2),
            DrawOnWindow = false
        }.InitTemplate();

        // CharacterGuideRo = new RecognitionObject
        // {
        //     Name = "CharacterGuide",
        //     RecognitionType = RecognitionTypes.TemplateMatch,
        //     TemplateImageMat = GameTaskManager.LoadAssetImage("AutoWood", "character_guide.png"),
        //     RegionOfInterest = new Rect(0, 0, CaptureRect.Width / 2, CaptureRect.Height),
        //     DrawOnWindow = false
        // }.InitTemplate();

        MenuBagRo = new RecognitionObject
        {
            Name = "MenuBag",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("AutoWood", "menu_bag.png", systemInfo),
            RegionOfInterest = new Rect(0, 0, CaptureRect.Width / 2, CaptureRect.Height),
            DrawOnWindow = false
        }.InitTemplate();

        ConfirmRo = new RecognitionObject
        {
            Name = "AutoWoodConfirm",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("AutoWood", "confirm.png", systemInfo),
            DrawOnWindow = false
        }.InitTemplate();

        EnterGameRo = new RecognitionObject
        {
            Name = "EnterGame",
            RecognitionType = RecognitionTypes.TemplateMatch,
            TemplateImageMat = GameTaskManager.LoadAssetImage("AutoWood", "exit_welcome.png", systemInfo),
            RegionOfInterest = new Rect(0, CaptureRect.Height / 2, CaptureRect.Width, CaptureRect.Height - CaptureRect.Height / 2),
            DrawOnWindow = false
        }.InitTemplate();
    }
}
