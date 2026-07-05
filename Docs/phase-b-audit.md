# Phase B Construction Chain Audit (Revised)

**Branch:** `mac-core-extraction` (commit `790225b`)
**Status:** Audit only — no code changes.

---

## 1. Two GameTaskManagers — Must NOT Be Confused

| Aspect | Windows Upstream (`BetterGenshinImpact/`) | Core Shim (`BetterGenshinImpact.Core/Shim/`) |
|--------|-----------------------------------------|---------------------------------------------|
| Type | `internal class GameTaskManager` | `public static class GameTaskManager` |
| LoadInitialTriggers | ✅ Yes — creates all triggers, including `new AutoPickTrigger()` | ❌ Not present |
| AddTrigger / LoadAssetImage | Both methods available | Only `LoadAssetImage` — trigger creation NOT in Core Shim |

**Key implication:** macOS Core does NOT call `LoadInitialTriggers()`. The statement "both Windows dispatcher and macOS runtime call this" is unproven. The macOS runtime needs its own trigger composition path, or must link GameTaskManager.

### macOS AutoPickTrigger Creation Sites (to be verified)

| Site | Existence | Notes |
|------|-----------|-------|
| Swift bridge | Unknown — not yet built | Likely wraps .NET dispatcher |
| macOS dispatcher | Unknown | Not yet implemented |
| Verification test | No | Current test creates RecordingInputBackend + DesktopRegion directly, no trigger |

**Conclusion:** macOS has no trigger-creation code yet. The planned `MacRuntimeAdapter` + composition root will be this code. Do not assume shared entry point with Windows.

---

## 2. AutoPickAssets — Must Be Addressed, Not Skiped

Current document said:

> Adapters do NOT need to change AutoPickAssets. It already accesses config through `TaskContext.Instance()`.

This is **wrong**. It contradicts Phase C (delete Shim). If AutoPickAssets continues to call `TaskContext.Instance().Config`, deleting the Shim breaks it.

### Consumer Analysis

All 8 call sites access `AutoPickAssets.Instance`, which calls `TaskContext.Instance().Config.AutoPickConfig` in its constructor.

Three options for injection:

| Option | Upstream Intrusion | Lifecycle Risk | Phase C Feasible? |
|--------|-------------------|----------------|-------------------|
| **A. Remove Singleton — explicit Init(provider)** | Moderate — change `Singleton<T>` base | Low — init once before first use | ✅ Yes — Shim still deletable |
| **B. Keep Singleton, populate via static provider setter** | Low — add one static method | Low (if setter required) | ✅ Yes — setter replaces `TaskContext.Instance()` |
| **C. AutoPickTrigger creates/takes its own AutoPickAssets instance** | Low — both files already coupled | None — no global state | ✅ Yes |

**Recommendation:** Option B (static provider setter on AutoPickAssets) has lowest upstream intrusion and preserves existing singleton lifecycle. Example:
```csharp
// AutoPickAssets.cs
public static IAutoPickConfigProvider? ConfigProvider { get; set; }
// constructor reads: _config = ConfigProvider ?? fallback to old TaskContext.Instance()
```

---

## 3. MacCoreRuntimeAdapter Must Cover All Three Interfaces

Phase A defined three interfaces:
- `IAutoPickConfigProvider` — AutoPick configuration
- `IOcrRuntimeConfigProvider` — OCR configuration
- `IAutoPickRuntimeState` — read-only `StopCount`

The adapter must provide **all three**, either as a single class or separate:
```csharp
class MacCoreRuntimeAdapter : IAutoPickConfigProvider, IOcrRuntimeConfigProvider { ... }
class MacAutoPickRuntimeState : IAutoPickRuntimeState { ... }
```

Do not rely on `RunnerContext.Instance()` after Phase C.

---

## 4. Static Gateway Is Banned — All References Deleted

Revision 3 prohibits: non-replaceable static global gateway.

The following suggestions from the previous document are **removed**:
- ❌ "static gateway" — prohibited
- ❌ "optional static method or thread-local" — prohibited
- ❌ "keep parameterless constructor but populate providers via static gateway" — prohibited
- ❌ "pre-populated container" — unacceptably ambiguous

### Allowed only:
- Constructor injection (preferred)
- Explicit factory parameter when constructor injection is infeasible
- Host composition root creates and wires dependencies — dispatcher consumes them

---

## 5. B1 Design Correction

**Original:** "Add providers to LoadInitialTriggers as out-params" — direction reversed.
**Corrected:** `LoadInitialTriggers(IAutoPickConfigProvider config, IAutoPickRuntimeState state)`

| Design | OK? | Reasoning |
|--------|-----|-----------|
| Explicit input parameters | ✅ | Clear dependency declaration |
| `ITriggerFactory` interface | ✅ | Replaceable; no static state |
| out-params | ❌ | Direction reversed |
| Pre-populated static container | ❌ | Global state by another name |
| Static gateway fallback | ❌ | Banned by Revision 3 |

---

## 6. OCR Config vs OCR Service — Must Not Be Confused

The previous document conflated:
- **IOcrRuntimeConfigProvider** — provides model config (PaddleOcrModelConfig + CultureInfo)
- **OcrFactory.Paddle** — returns `IOcrService` instance for running OCR

IOcrRuntimeConfigProvider does NOT replace OcrFactory.Paddle. Two separate dependencies:

### OCR Dependency Paths

```
OcrFactory (DI singleton)
  ├─ requires: IOcrRuntimeConfigProvider  (model type + culture)
  ├─ requires: BgiOnnxFactory             (ONNX inference session)
  └─ provides: IOcrService via OcrFactory.Paddle static
       └─ consumed by: AutoPickTrigger, AutoBoss, AutoFight, ...

PickTextInference
  └─ requires: BgiOnnxFactory             (ONNX inference session)
```

### Phase B Scope for OCR

| Dependency | Phase B Treatment |
|------------|-------------------|
| `IOcrRuntimeConfigProvider` → OcrFactory constructor | Add as constructor parameter |
| `BgiOnnxFactory` → OcrFactory/PickTextInference | Keep as DI-injected; not a config concern |
| `OcrFactory.Paddle` static → callers | Defer — 20+ call sites, large blast radius |

Result: OcrFactory takes `IOcrRuntimeConfigProvider` in constructor. `PaddleOcrService` keeps getting `BgiOnnxFactory` from DI. `OcrFactory.Paddle` static remains untouched in Phase B.

---

## 7. Global Dependencies: Precise Counting

| Category | Count | Locations |
|----------|-------|-----------|
| `App.ServiceProvider.GetRequiredService<T>()` | **3** | OcrFactory.Paddle, OcrFactory.CreatePaddleOcr (2×), PickTextInference ctor |
| `TaskContext.Instance().Config.*` — static singleton | **4 active in Core** | AutoPickTrigger:2, AutoPickAssets:2 |
| `RunnerContext.Instance` — static singleton | **1 active in Core** | AutoPickTrigger:1 |

**Do not merge these counts.** Different remedies needed:
- `App.ServiceProvider` → constructor injection or factory
- `TaskContext/RunnerContext` → per-consumer interfaces + adapter

---

## 8. Composition Root Location

### Correct assignment:

| Platform | Composition Root | Responsibility |
|----------|-----------------|----------------|
| Windows | `App.OnStartup` + DI container | Registers all services, creates dispatcher |
| macOS | Swift bridge + .NET runtime bootstrap | Creates adapters, configures providers, creates dispatcher |

**Dispatcher:** Receives fully-assembled dependencies. Does NOT create adapters or register services. Its job is to run the tick loop.

---

## 9. Revised Phase B Steps (B1-B5)

### B1: Add IOcrRuntimeConfigProvider to OcrFactory constructor

- Add `IOcrRuntimeConfigProvider` parameter to `OcrFactory(ILogger, IOcrRuntimeConfigProvider)`
- Replace `GetConfig()` body with provider access
- On Windows: DI container resolves provider from existing TaskContext
- On macOS: adapter provides the config
- **Still works:** OcrFactory.Paddle static + BgiOnnxFactory service locator unchanged

### B2: Add IAutoPickConfigProvider setter to AutoPickAssets

- Add static setter `AutoPickAssets.ConfigProvider { get; set; }`
- Constructor: if setter is populated, use it instead of `TaskContext.Instance()`
- Windows: setter populated by TaskTriggerDispatcher after Init
- macOS: setter populated by composition root before first Instance access
- **Still works:** No constructor change; singleton pattern preserved

### B3: Add IAutoPickRuntimeState to AutoPickTrigger

- Add `IAutoPickRuntimeState` parameter to `AutoPickTrigger(IAutoPickRuntimeState)` overload
- Parameterless constructor keeps `RunnerContext.Instance` for backward compat
- Windows: uses parameterless constructor (no change)
- macOS: uses new overload with adapter-provided state
- **Still works:** Both creation paths functional; single code path on each platform

### B4: Create MacCoreRuntimeAdapter

- Implements `IAutoPickConfigProvider` + `IOcrRuntimeConfigProvider`
- Lives in `BetterGenshinImpact.Core/Adapters/`
- Receives: `AutoPickConfig`, `OtherConfig.Ocr`, `PaddleOcrModelConfig`
- macOS composition root creates one instance, wires into B1-B3
- Also creates `MacAutoPickRuntimeState` for `IAutoPickRuntimeState`

### B5: Delete Shim Files (conditional)

Only possible when:
- No linked file accesses `TaskContext.Instance()` for AutoPick/OCR config
- All consumers use per-consumer interfaces
- AutoPickAssets.ConfigProvider setter is always populated before first access

**Until then: Shims stay.** Delete only after each direct caller count reaches zero.

---

## 10. Service Locator Cleanup — Deferred to Phase C

These are NOT addressed in Phase B:
- `OcrFactory.Paddle` static (20+ call sites in Windows host)
- `PickTextInference` constructor (uses `App.ServiceProvider.GetRequiredService<BgiOnnxFactory>()`)

Phase C will:
- Replace `OcrFactory.Paddle` static with injected `IOcrService`
- Inject `BgiOnnxFactory` into `PickTextInference`
- Remove `App.ServiceProvider` calls from recognition code

Phase B's goal is to **add config providers** — not eliminate all service locators.

---

## Summary: Phase B Feasibility

| Step | Scope | Feasible Now? | Prerequisite |
|------|-------|---------------|--------------|
| B1 | IOcrRuntimeConfigProvider → OcrFactory ctor | ✅ Yes | Phase A interfaces exist |
| B2 | IAutoPickConfigProvider setter → AutoPickAssets | ✅ Yes | Minimal intrusive change |
| B3 | IAutoPickRuntimeState → AutoPickTrigger overload | ✅ Yes | Optional param keeps backward compat |
| B4 | MacCoreRuntimeAdapter | ✅ Yes | B1-B2 interfaces exist |
| B5 | Delete Shim files | ❌ No | Direct caller count must reach 0 |

**B1-B4 can proceed independently.** B5 is gated on zero direct callers.
