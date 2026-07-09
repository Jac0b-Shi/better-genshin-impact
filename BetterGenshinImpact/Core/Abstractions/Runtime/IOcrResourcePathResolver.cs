using BetterGenshinImpact.Core.Recognition.ONNX;

namespace BetterGenshinImpact.Core.Abstractions.Runtime;

/// <summary>
/// Resolves PaddleOCR resource paths (models, config files, preheat images, labels).
/// Sidecar resources (inference.yml, character_dict, PNG) are not ONNX models —
/// this interface is kept separate from <see cref="IOnnxModelPathResolver"/>.
/// </summary>
public interface IOcrResourcePathResolver
{
    string ResolveModelPath(BgiOnnxModel model);
    string ResolveModelDirectory(BgiOnnxModel model);
    string ResolveSidecarPath(string relativePath);
}
