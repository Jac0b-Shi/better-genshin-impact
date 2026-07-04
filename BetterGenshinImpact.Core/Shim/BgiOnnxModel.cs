namespace BetterGenshinImpact.Core.Recognition.ONNX;

/// <summary>
/// Cross-platform ONNX model registry. Mirrors the upstream BgiOnnxModel static instances.
/// The upstream uses private init setters; this version provides public construction
/// for Core + retains the same static model references that OCR/task code depends on.
/// </summary>
public class BgiOnnxModel
{
    public string Name { get; set; } = "";
    public string ModelRelativePath { get; set; } = "";
    public string ModalPath => ModelRelativePath;
    public string CacheRelativePath { get; set; } = "";
    public string CachePath => CacheRelativePath;

    // -- Static model instances (match upstream naming) --

    public static readonly BgiOnnxModel YapModelTraining = new()
    {
        Name = "YapModelTraining",
        ModelRelativePath = @"Assets\Model\Yap\model_training.onnx"
    };

    public static readonly BgiOnnxModel PaddleOcrDetV4 = new()
    {
        Name = "PaddleOcrDetV4",
        ModelRelativePath = @"Assets\Model\PaddleOcr\ppocr_det_v4.onnx"
    };

    public static readonly BgiOnnxModel PaddleOcrDetV5 = new()
    {
        Name = "PaddleOcrDetV5",
        ModelRelativePath = @"Assets\Model\PaddleOcr\ppocr_det_v5.onnx"
    };

    public static readonly BgiOnnxModel PaddleOcrDetV6 = new()
    {
        Name = "PaddleOcrDetV6",
        ModelRelativePath = @"Assets\Model\PaddleOcr\ppocr_det_v6.onnx"
    };

    public static readonly BgiOnnxModel PaddleOcrRecV4 = new()
    {
        Name = "PaddleOcrRecV4",
        ModelRelativePath = @"Assets\Model\PaddleOcr\ppocr_rec_v4.onnx"
    };

    public static readonly BgiOnnxModel PaddleOcrRecV4En = new()
    {
        Name = "PaddleOcrRecV4En",
        ModelRelativePath = @"Assets\Model\PaddleOcr\ppocr_rec_v4_en.onnx"
    };

    public static readonly BgiOnnxModel PaddleOcrRecV5 = new()
    {
        Name = "PaddleOcrRecV5",
        ModelRelativePath = @"Assets\Model\PaddleOcr\ppocr_rec_v5.onnx"
    };

    public static readonly BgiOnnxModel PaddleOcrRecV5Latin = new()
    {
        Name = "PaddleOcrRecV5Latin",
        ModelRelativePath = @"Assets\Model\PaddleOcr\ppocr_rec_v5_latin.onnx"
    };

    public static readonly BgiOnnxModel PaddleOcrRecV5Eslav = new()
    {
        Name = "PaddleOcrRecV5Eslav",
        ModelRelativePath = @"Assets\Model\PaddleOcr\ppocr_rec_v5_eslav.onnx"
    };

    public static readonly BgiOnnxModel PaddleOcrRecV5Korean = new()
    {
        Name = "PaddleOcrRecV5Korean",
        ModelRelativePath = @"Assets\Model\PaddleOcr\ppocr_rec_v5_korean.onnx"
    };

    public static readonly BgiOnnxModel PaddleOcrRecV6 = new()
    {
        Name = "PaddleOcrRecV6",
        ModelRelativePath = @"Assets\Model\PaddleOcr\ppocr_rec_v6.onnx"
    };
}
