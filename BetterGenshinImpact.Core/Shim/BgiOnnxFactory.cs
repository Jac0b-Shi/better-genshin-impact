using BetterGenshinImpact.Core.Recognition.ONNX;
using Microsoft.ML.OnnxRuntime;

namespace BetterGenshinImpact.Core.Recognition.ONNX;

/// <summary>
/// Core/macOS ONNX model factory: CPU-only InferenceSession creation.
/// See WPF authoritative for GPU/CUDA/TensorRT-enabled version.
/// </summary>
public class BgiOnnxFactory
{
    public InferenceSession CreateInferenceSession(BgiOnnxModel model, bool ocr = false)
    {
        return new InferenceSession(model.ModelRelativePath);
    }
}
