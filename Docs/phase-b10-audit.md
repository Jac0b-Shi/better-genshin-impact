# B10 Audit: Shim Inventory and Classification

**Status:** Audit only — no code deleted
**Predecessor:** B9 complete (commit `f381378`, Core Verification 106/106)

---

## 1. Core Assembly Boundary

`BetterGenshinImpact.Core` compiles the following (per `Core.csproj`, `EnableDefaultCompileItems=false`):

| Layer | Count | Source |
|-------|-------|--------|
| Linked upstream files | ~60 | `BetterGenshinImpact/` via `<Compile Include=... Link=...>` |
| Shim files | **20** | `Shim/*.cs` via `<Compile Include="Shim/...">` |
| Adapters | 4 | `Adapters/` (MacCoreRuntime, MacAutoPick, 2 Unsupported) |
| Composition | 1 | `Composition/MacAutoPickComposition.cs` |

**NOT compiled into Core:**
- `BetterGenshinImpact/GameTask/GameTaskManager.cs` (Windows upstream — NOT linked)
- `BetterGenshinImpact/GameTask/TaskTriggerDispatcher.cs` (Windows — NOT linked)
- `BetterGenshinImpact/Core/Runtime/Windows/` (Windows DI, adapters, backend — NOT compiled)
- `BetterGenshinImpact/App.xaml.cs` (DI — NOT compiled)

Therefore:

| Claim | Evidence |
|-------|----------|
| TaskTriggerDispatcher calls upstream GameTaskManager | **WPF only** — neither is compiled in Core |
| Core/MacOS uses upstream GameTaskManager | **False** — Core compiles Shim/GameTaskManager.cs |
| `LoadInitialTriggers` exists in Core | **False** — only `LoadAssetImage` and `AddTrigger` exist in shim |
| MacAutoPickComposition bypasses GameTaskManager | **True** — constructs AutoPickTrigger directly |

---

## 2. Shim Classification (mutually exclusive)

### A. B10.1 attempted deletion — all 6 candidates have real references in linked files

Trial deletion of the six shims identified as "zero references" in the initial audit
revealed that all are required by linked upstream files compiled into Core:

| File | Linked consumer(s) | Resolution |
|------|-------------------|------------|
| `BvStubs.cs` | `ITaskTrigger.cs` (namespace `BetterGenshinImpact.GameTask.Common.BgiVision`), `CaptureContent.cs` | **Keep** — namespace import in linked files |
| `CoreExtensions.cs` | `ImageRegion.cs` [ClampTo], `RecognitionObject.cs` [ToScalar] | **Keep** — extension methods used by linked files |
| `DrawableStubs.cs` | `Region.cs` [DrawContent], `ImageRegion.cs` [DrawContent, VisionContext, RemoveRect] | **Keep** — DrawContent type + VisionContext + methods used by linked files |
| `GameUiCategory.cs` | `ITaskTrigger.cs`, `CaptureContent.cs` | **Keep** — enum used by linked files |
| `StringUtils.cs` | `ImageRegion.cs` [StringUtils.RemoveAllSpace] | **Keep** — extension method used by linked file |
| `TaskControl.cs` | `Region.cs`, `ImageRegion.cs` [TaskControl.Logger, CaptureToRectArea, Sleep] | **Keep** — static helper used by linked files |

**Conclusion: Zero shim files can be safely deleted from the current 20.** All 20 shims are still referenced
by at least one linked upstream file compiled into Core. The audit's initial "zero references" claim
was incorrect — it missed references in Area/Recognition/Config linked files outside the AutoPick chain.

B10.1 is **not viable** at this time. No further B10.1 work should proceed unless linked upstream files
are modified to remove their dependency on these types.

### C. Production compatibility shim — required until upstream dependency removed

| File | Core consumers | Why required |
|------|---------------|--------------|
| `App.cs` | `AutoPickTrigger` (logger), `AutoPickAssets` (logger), `OcrFactory` (ServiceProvider) | No WPF-free cross-platform `ILogger` resolver |
| `BgiOnnxFactory.cs` | `PickTextInference` (via linked `OcrFactory`) | ONNX engine construction |
| `BgiOnnxModel.cs` | `BgiOnnxFactory` | Model lifecycle |
| `Global.cs` | `AutoPickTrigger` (3× `ReadAllTextIfExist`) | File I/O abstraction |
| `Simulation.cs` | Via `KeyboardFacade`/`MouseFacade` — delegates to `PlatformServices.Input` | Wraps IInputBackend as static facade; used by linked files |
| `SpeedTimer.cs` | `AutoPickTrigger.OnCapture` (debug perf) | Logging helper |
| `RunnerContext.cs` | `AutoPickTrigger.StopCount` fallback | One field (`AutoPickTriggerStopCount`) |
| `TaskContext.cs` | Linked `BaseAssets`, `OcrFactory`, `AutoPickAssets` (Core-compiled); `TaskTriggerDispatcher` (WPF-only) | Provides Config + SystemInfo stub for Core/macOS |
| `ThemedMessageBox.cs` | `AutoPickTrigger` (3× `.Error()`) | UI dialog stub |
| `PlatformServices.cs` | `DesktopRegion` (linked — 5 calls), `Simulation` (shim), `Verification` (test setup) | Static IInputBackend gateway; required by DesktopRegion in Core |
| `MacSystemInfo.cs` | Shim `TaskContext.cs` constructs it: `SystemInfo = new MacSystemInfo()` | Not test-owned — TaskContext shim is a production Core assembly dependency. Cannot move to Test until: TaskContext no longer default-constructs it, ISystemInfo is injected by host/composition, and all Core-compiled callers are migrated. |

### D. Test-owned — should move to Test project

*(Currently empty — no shim is purely test-owned. MacSystemInfo appears test-owned but is constructed by production Shim/TaskContext.cs.)*

---

## 3. GameTaskManager Deep Dive

### 3.1 Core vs WPF boundary

```
WPF assembly (BetterGenshinImpact):
  TaskTriggerDispatcher (native file)
  → BetterGenshinImpact/GameTask/GameTaskManager.cs (native)
  → full dispatch lifecycle

Core assembly (BetterGenshinImpact.Core):
  MacAutoPickComposition.Compose
  → directly constructs AutoPickTrigger (no GameTaskManager)
  AutoPickAssets.InitTemplateAssets
  → Shim/GameTaskManager.LoadAssetImage(...)
  Verification
  → Shim/GameTaskManager.AddTrigger(...)
```

### 3.2 Shim vs upstream AddTrigger

| Aspect | Windows upstream (WPF) | Core shim |
|--------|------------------------|-----------|
| Trigger types | AutoPick + AutoSkip + AutoEat | AutoPick only |
| `LoadInitialTriggers` | Full lifecycle load | **Not present** |
| `ConvertToTriggerList` | Init + Priority sort | Simple Value filter |
| `ReloadAssets` | Destroys + reloads all assets | **Not present** |
| `AddTrigger("AutoPick")` | Full constructor | Used only by Verification |

The shim `AddTrigger` is **not used by production Core or macOS code**. Only Verification tests call it. It exists to provide a verifiable entry point for the composition chain.

### 3.3 Recommendations

- Keep shim `LoadAssetImage` and `AddTrigger` for their respective consumers
- Do not attempt to link the Windows upstream `GameTaskManager.cs` — it pulls in 12 task types with dozens of Windows asset dependencies
- No change needed for B10

---

## 4. Deletion Plan

| Batch | Scope | Files | Verification |
|-------|-------|-------|-------------|
| **B10.1** | Pure dead shim | BvStubs, CoreExtensions, DrawableStubs, GameUiCategory, StringUtils, TaskControl (6 files) | Core Verification 106/106 |
| B10.2 | Evaluation after B10.1 | Remaining 14 shims — each requires upstream dependency audit | TBD |
| B10.next | *Future* — not planned in detail | App, Global, ConfigService, ThemedMessageBox, RunnerContext, SpeedTimer | Blocked by AutoPickTrigger upstream dependencies |
| B10.next | *Future* — BgiKeyMapper | Replace shim with linked `Helpers/BgiKeyMapper.cs` | Verify pure mapping compiles |
| B10.next | *Future* — MacSystemInfo | Move to Test project if production macOS composition no longer uses TaskContext shim | Verify 106/106 |

---

## 5. Verification Baseline

| Metric | Current |
|--------|---------|
| Core Verification | **106/106** |
| Core build errors | Zero |
| WPF B9 type resolution | Zero B9-type errors |
| Shim files compiled | 20 |
| Adapter-gate | Not triggered (no adapter file changes) |
