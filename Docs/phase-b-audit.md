## Phase B Construction Chain Audit

### B1. AutoPickTrigger Construction Chain

| Site | Line | Mechanism | Impact |
|------|------|-----------|--------|
| `GameTaskManager.LoadInitialTriggers()` | `GameTaskManager.cs:47` | `new AutoPick.AutoPickTrigger()` — parameterless constructor | **Primary creation path.** Both Windows dispatcher and macOS runtime call this. |
| `GameTaskManager.AddTrigger("AutoPick", externalConfig)` | `GameTaskManager.cs:97` | `new AutoPickTrigger(externalConfig as AutoPickExternalConfig)` | Script-driven trigger creation. Less common. |

**Trigger initialization flow:**
```
TaskTriggerDispatcher.Start()
  → _triggers = GameTaskManager.LoadInitialTriggers()
    → new AutoPickTrigger()
      → AutoPickTrigger.Init() reads TaskContext.Instance().Config.AutoPickConfig
      → AutoPickAssets.Instance  (Singleton<AutoPickAssets>)
```
All triggers are created at dispatcher start. AutoPickTrigger is **not** DI-injected on Windows — it's `new`'d directly by a static method.

**If we add two constructor parameters (IAutoPickConfigProvider, IAutoPickRuntimeState):**
- `GameTaskManager.LoadInitialTriggers()` — must be updated to pass providers
- `GameTaskManager.AddTrigger("AutoPick", externalConfig)` — must also pass providers
- One option: keep parameterless constructor but populate providers before/after construction via a static gateway

### B2. AutoPickAssets Singleton

| Property | Detail |
|----------|--------|
| Base class | `Singleton<AutoPickAssets>` (via `Model/Singleton<T>`) |
| Instance access | `AutoPickAssets.Instance` — lazy-initialized on first access |
| Init timing | On first access, constructor calls `TaskContext.Instance().Config.AutoPickConfig.PickKey` |
| Dependency order | `TaskContext` must be initialized BEFORE first call to `AutoPickAssets.Instance` |

All 8 call sites in linked files access `AutoPickAssets.Instance`:
- `AutoPickTrigger.Init()` — initializes black/white lists
- `AutoPickTrigger.OnCapture()` — reads `PickRo`, `PickVk`
- `BvSimpleOperation.cs:131,175,186` — picks objects
- `BvOcr.cs:20` — OCR
- `WalkToFTask.cs:33` — pathing
- `AutoFightJsonTask.cs:849`, `AutoFightTask.cs:739` — combat (not linked)

**Adapters do NOT need to change AutoPickAssets.** It already accesses config through `TaskContext.Instance()`. The adapter pattern replaces the config source behind `TaskContext.Instance().Config` — AutoPickAssets reads from whatever TaskContext provides.

### B3. OcrFactory Construction Chain

| Site | Line | Mechanism |
|------|------|-----------|
| DI registration | `App.xaml.cs:162` | `services.AddSingleton<OcrFactory>()` |
| Static access | `OcrFactory.cs:18` | `App.ServiceProvider.GetRequiredService<OcrFactory>().PaddleOcr` |
| Instance constructor | `OcrFactory.cs:41` | `OcrFactory(ILogger<BgiOnnxFactory> logger)` — reads config from `GetConfig()` (which calls `TaskContext.Instance().Config.OtherConfig`) |

**PaddleOcrService construction** (inside `CreatePaddleOcrInstance()`):
```
OcrFactory._config.PaddleOcrModelConfig switch {
    V4Auto => new PaddleOcrService(App.ServiceProvider.GetRequiredService<BgiOnnxFactory>(), ...),
    V5Auto => new PaddleOcrService(App.ServiceProvider.GetRequiredService<BgiOnnxFactory>(), ...),
    V5     => new PaddleOcrService(App.ServiceProvider.GetRequiredService<BgiOnnxFactory>(), ...),
    V6     => new PaddleOcrService(App.ServiceProvider.GetRequiredService<BgiOnnxFactory>(), ...),
}
```

**Service locator calls remaining after IOcrRuntimeConfigProvider injection:**

| Call | Location | Remedy |
|------|----------|--------|
| `App.ServiceProvider.GetRequiredService<OcrFactory>()` | `OcrFactory.cs:18` (static `Paddle` property) | Must be replaced — this is how every OCR consumer gets the instance |
| `App.ServiceProvider.GetRequiredService<BgiOnnxFactory>()` | `OcrFactory.cs:90-104` (inside `CreatePaddleOcrInstance`) | Must be injected into `OcrFactory` or refactored |
| `App.ServiceProvider.GetRequiredService<BgiOnnxFactory>()` | `PickTextInference.cs:28` (constructor) | Must be injected |

**OcrFactory.Paddle call sites in ALL upstream files: 20+ locations** (AutoBoss, AutoFight, AutoEat, AutoGeniusInvokation, AutoArtifactSalvage, etc.)
**In linked Core files: 2 locations** (`AutoPickTrigger.cs:291,312`)

### B4. BgiOnnxFactory Construction Chain

| Site | Line | Mechanism |
|------|------|-----------|
| DI registration | `App.xaml.cs:161` | `services.AddSingleton<BgiOnnxFactory>()` |
| Constructor | `BgiOnnxFactory.cs:33` | `BgiOnnxFactory(ILogger<BgiOnnxFactory> logger)` |
| Consumed by | `OcrFactory`, `PaddleOcrService(Det/Rec)`, `PickTextInference`, `BgiYoloPredictor` |

### B5. Adapter Design: Summary

**MacCoreRuntimeAdapter** — implements `IAutoPickConfigProvider` + `IOcrRuntimeConfigProvider`
- Lives in `BetterGenshinImpact.Core/` (can reference Shims/implementations)
- Inputs: `MacSystemInfo`, `AutoPickConfig`, `OtherConfig.Ocr`
- Lifecycle: created at dispatcher start, held by composition root or static gateway

**WindowsCoreRuntimeAdapter** — implements the same interfaces
- Lives in Windows host project (`BetterGenshinImpact/`)
- Backed by real upstream `TaskContext.Instance()` and `ConfigService`
- Inputs: `TaskContext`, `AllConfig`
- NOT linked into Core — only the interface reference crosses the project boundary

**Composition root:** `TaskTriggerDispatcher` (on both platforms)
- Windows: `TaskContext.Instance().Init(hWnd)` → create WindowsCoreRuntimeAdapter
- macOS: Create MacSystemInfo + CoreConfig → create MacCoreRuntimeAdapter
- Both register adapters into a Core-local runtime services container (NOT a static singleton)
- Then call `GameTaskManager.LoadInitialTriggers()` which creates triggers

### B6. Phase B Small-Step Commit Boundaries

| Phase | Step | Files Changed | Verification |
|-------|------|---------------|--------------|
| B1 | Add `IAutoPickConfigProvider` / `IOcrRuntimeConfigProvider` to `GameTaskManager.LoadInitialTriggers` as out-params or use a pre-populated container | `GameTaskManager.cs` | 0 errors, 15/15 |
| B2 | Create `MacCoreRuntimeAdapter` in Core/Adapters/, wire into current Shims | New file(s) | 0 errors, 15/15 |
| B3 | Modify `AutoPickTrigger` to accept providers via optional static method or thread-local — do NOT change constructor yet | `AutoPickTrigger.cs` | 0 errors, 15/15 |
| B4 | Modify `OcrFactory.Paddle` static accessor to use injected provider (or keep service locator but add fallback) | `OcrFactory.cs` | 0 errors, 15/15 |
| B5 | Delete Shim files that are no longer referenced | Multiple | 0 errors, 15/15 |

**Note:** B3-B5 require deeper design decisions (constructor injection vs static fallback vs composition root). These should be addressed in individual design spikes, not batched.

### B7. Current Service Locator Footprint (post-IOcrRuntimeConfigProvider)

```
OcrFactory.Paddle     → App.ServiceProvider.GetRequiredService<OcrFactory>()
PickTextInference()   → App.ServiceProvider.GetRequiredService<BgiOnnxFactory>()
AutoPickTrigger       → TaskContext.Instance().Config.AutoPickConfig
RunnerContext.StopCount → RunnerContext.Instance.AutoPickTriggerStopCount
```

Phase B must eliminate at least the first two (or document why they stay). The third is eliminated by implementing `IAutoPickConfigProvider` in the adapter. The fourth is eliminated by `IAutoPickRuntimeState`.
