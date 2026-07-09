# B11 Audit: Platform Capability Wiring

**Status:** Audit only — no code or config changes
**Predecessor:** B10 structural shim cleanup complete

---

## 1. B11.1 Audit: BgiOnnxModel Deployment and Path Resolution

### 1.1 Current state

Core BgiOnnxModel defines 11 static ONNX model entries with hardcoded relative paths:

| Model | Core path (`ModelRelativePath`) |
|-------|-------------------------------|
| `YapModelTraining` | `Assets\Model\Yap\model_training.onnx` |
| `PaddleOcrDetV4` | `Assets\Model\PaddleOcr\ppocr_det_v4.onnx` |
| `PaddleOcrDetV5` | `Assets\Model\PaddleOcr\ppocr_det_v5.onnx` |
| `PaddleOcrDetV6` | `Assets\Model\PaddleOcr\ppocr_det_v6.onnx` |
| `PaddleOcrRecV4` | `Assets\Model\PaddleOcr\ppocr_rec_v4.onnx` |
| `PaddleOcrRecV4En` | `Assets\Model\PaddleOcr\ppocr_rec_v4_en.onnx` |
| `PaddleOcrRecV5` | `Assets\Model\PaddleOcr\ppocr_rec_v5.onnx` |
| `PaddleOcrRecV5Latin` | `Assets\Model\PaddleOcr\ppocr_rec_v5_latin.onnx` |
| `PaddleOcrRecV5Eslav` | `Assets\Model\PaddleOcr\ppocr_rec_v5_eslav.onnx` |
| `PaddleOcrRecV5Korean` | `Assets\Model\PaddleOcr\ppocr_rec_v5_korean.onnx` |
| `PaddleOcrRecV6` | `Assets\Model\PaddleOcr\ppocr_rec_v6.onnx` |

Core `BgiOnnxFactory.CreateInferenceSession(model)` uses `model.ModelRelativePath` directly — a raw relative path resolved against the process working directory.

### 1.2 Call chain

``` 
BgiOnnxFactory.CreateInferenceSession(model, ocr)
  → new InferenceSession(model.ModelRelativePath)
    → loaded by Det.cs:14, Rec.cs:22, PickTextInference.cs:28
```

All 3 callers are Core-linked and runtime-reachable from the macOS OCR pipeline.

### 1.3 WPF comparison

| Aspect | Core shim | WPF authoritative |
|--------|-----------|-------------------|
| Path resolution | `ModelRelativePath` (raw relative) | `ModalPath` → `Global.Absolute(ModelRelativePath)` |
| Session creation | `new InferenceSession(path)` | `new InferenceSession(path)` (same API) |
| Model source | External (not in repo) | External (not in repo) |
| Copy rules | **None** in Core csproj | **None** for `.onnx` in WPF csproj |
| ONNX Runtime package | `Microsoft.ML.OnnxRuntime 1.21.0` | Same + `DirectML` |

### 1.4 Model file availability

| Check | Result |
|-------|--------|
| `.onnx` files in repository? | **Zero** — no model files committed |
| `.gitignore` excludes them? | **No** — no onnx pattern in .gitignore |
| csproj copies them to output? | **No** — no `CopyToOutputDirectory` rules for `.onnx` in either csproj |
| WPF production loads them? | Yes — via `Global.Absolute()` resolving to publish directory or dev root |
| Core production can load them? | **No** — raw `ModelRelativePath` resolves against working directory; no files exist at any expected path |

**The `.onnx` model files are external runtime artifacts** in both WPF and Core projects. The WPF project relies on `Global.Absolute()` to resolve paths; the Core shim has no equivalent path-resolution mechanism. Even if model files were placed at the expected paths, the working directory would need to match.

### 1.5 Failure mode

`InferenceSession` construction will fail when the model path cannot be resolved. The exact exception type (`OnnxRuntimeException` or `FileNotFoundException`) has not been verified — no test in the current 112/112 suite creates an `InferenceSession` or loads an `.onnx` file.

| Scenario | Result |
|----------|--------|
| `dotnet run` from repo root | `./Assets/Model/...` does not exist → session creation fails |
| `dotnet test` from any directory | Same — no model files in test output |
| macOS .NET host with published app | Same — no copy rule includes `.onnx` files |
| Swift host calling into .NET runtime | Same — model files not bundled |

### 1.6 Resolution options

| Option | Description | Impact on WPF | Core build | Verification | Swift host | Preferred? |
|--------|-------------|---------------|------------|--------------|------------|------------|
| **A** — Add `Global.Absolute` to Core BgiOnnxModel | Change `ModalPath` from `ModelRelativePath` to `Global.Absolute(ModelRelativePath)` matching WPF | None (different file) | Minimal — `Global` already exists in Core | Unchanged (doesn't load models) | Requires model files at project-relative paths | ✅ **Recommended** |
| **B** — Add copy rules to Core csproj | Copy `.onnx` glob pattern to output directory | None | Adds dependency on model file availability | Model files needed in test output | Same issue | Not without model files |
| **C** — macOS bundle resources | Model files in `.app/Contents/Resources`; Swift host passes resource root | None | No change | No change | Must pass resource root | Phase 2 |
| **D** — Download/verify script | Script pulls models from upstream release | None | Requires network | Same | Same | Prerequisite for all |

### 1.7 Recommended plan (B11.1.1)

**Step 1: Change Core BgiOnnxModel.ModalPath to use Global.Absolute**

This mirrors the WPF authoritative behavior. `Global.Absolute()` already exists in the Core shim and resolves paths relative to the project root by walking up the directory tree.

```csharp
// Before:
public string ModalPath => ModelRelativePath;
// After:
public string ModalPath => Global.Absolute(ModelRelativePath);
```

Same for `CachePath`.

**Impact:**
- `BgiOnnxFactory.CreateInferenceSession(model)` currently uses `model.ModelRelativePath`, not `model.ModalPath`. If the change is to `ModalPath`, the factory call site must also change from `model.ModelRelativePath` to `model.ModalPath`.
- Core build: 0 errors expected — `Global` namespace already imported
- Verification: 112/112 — model files not loaded
- WPF: None — Core shim is a separate file

**Step 2: Change BgiOnnxFactory.CreateInferenceSession to use model.ModalPath**

In `Shim/BgiOnnxFactory.cs`:
```csharp
// Before:
return new InferenceSession(model.ModelRelativePath);
// After:
return new InferenceSession(model.ModalPath);
```

This ensures the session constructor receives an absolute path resolved by `Global.Absolute()`.

**Step 3: Add model download/availability to Verification**

Not in current B11.1 scope. Verification continues to validate wiring only; model loading remains uncovered.

### 1.8 Risks

| Risk | Severity | Mitigation |
|------|----------|------------|
| `Global.Absolute` may resolve to wrong path in published/swift-hosted scenarios | **Medium** — `Global.Absolute` walks up from `BaseDirectory` looking for `BetterGenshinImpact` directory, which may not exist in a published app | Document as known limitation; Option C (bundle resources) addresses it |
| Model files still absent — path resolution fix alone doesn't make OCR work | **High** — models are external artifacts | Separate from path resolution. Document as known blocker. |
| `Global.Absolute` uses `\` path separator on Windows — may not work on macOS if backslashes are hardcoded in ModelRelativePath | **Low** — `Global.Absolute` calls `Path.Combine` which handles both separators. `ModelRelativePath` uses `\` but `Path.Combine` normalizes on macOS. Verified by existing `Global.Absolute(@"Assets\Model\PaddleOcr\...")` pattern. | Confirm with test. |

### 1.9 Call site changes required

| File | Current | After |
|------|---------|-------|
| `Shim/BgiOnnxFactory.cs:20` | `new InferenceSession(model.ModelRelativePath)` | `new InferenceSession(model.ModalPath)` |
| `Shim/BgiOnnxModel.cs:12` | `public string ModalPath => ModelRelativePath` | `public string ModalPath => Global.Absolute(ModelRelativePath)` |
| `Shim/BgiOnnxModel.cs:14` | `public string CachePath => CacheRelativePath` | `public string CachePath => Global.Absolute(CacheRelativePath)` |

### 1.10 Baseline validation

```
dotnet build BetterGenshinImpact.Core/BetterGenshinImpact.Core.csproj  → zero errors ✅
dotnet run --project Test/BetterGenshinImpact.Core.Verification/...    → 112/112 ✅
```

### 1.11 Items deferred outside B11.1

- Actual `.onnx` model file deployment (externally managed)
- Model loading coverage in Verification
- macOS bundle resource strategy (Option C)
- WPF model path alignment (`PaddleOcr` vs `PaddleOCR/Det|Rec/V{n}/...` directory structure)
