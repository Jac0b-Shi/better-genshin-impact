using BetterGenshinImpact.Core.Recognition.ONNX;
using Microsoft.ML.OnnxRuntime;

namespace BetterGenshinImpact.Core.Recognition.ONNX;

/// <summary>
/// TEMPORARY VERIFICATION SHIM: ONNX model factory.
/// </summary>
public class BgiOnnxFactory
{
    public BgiOnnxModel GetPpOcrV3DetModel() => BgiOnnxModel.PaddleOcrDetV4;
    public BgiOnnxModel GetPpOcrV4DetModel() => BgiOnnxModel.PaddleOcrDetV4;
    public BgiOnnxModel GetPpOcrV3RecModel() => BgiOnnxModel.PaddleOcrRecV4;
    public BgiOnnxModel GetPpOcrV4RecModel() => BgiOnnxModel.PaddleOcrRecV4;
    public BgiOnnxModel GetPpOcrV5RecModel() => BgiOnnxModel.PaddleOcrRecV5;
    public BgiOnnxModel GetSVTRPickModel() => BgiOnnxModel.YapModelTraining;

    public InferenceSession CreateInferenceSession(BgiOnnxModel model, bool ocr = false)
    {
        return new InferenceSession(model.ModelRelativePath);
    }
}
