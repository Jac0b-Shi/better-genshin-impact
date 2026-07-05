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

AutoPickAssets has a **private** constructor (CRTP singleton pattern: `BaseAssets<AutoPickAssets>` via `Singleton<T>`).
Normal constructor injection is not possible without breaking the singleton base.

### Recommended: Explicit Configure() — Not Static Setter

A static setter (`AutoPickAssets.ConfigProvider`) is **also a static gateway** — it has the same problems:
- Initialization order dependency
- Cross-test pollution
- No isolation for multi-runtime scenarios
- Will remain after Shim deletion, contradicting the static-gateway ban (§4)

**Correct approach — explicit Configure() method:**

```csharp
// AutoPickAssets.cs (unchanged singleton)
private IAutoPickConfigProvider? _configProvider;
private bool _configured;

public void Configure(IAutoPickConfigProvider provider)
{
    if (_configured) throw new InvalidOperationException("Already configured");
    _configProvider = provider;
    _configured = true;
}
```

| Property | Value |
|----------|-------|
| Invoked by | Composition root or trigger factory, after Instance first access |
| Must complete before | First OnCapture() call |
| Must be idempotent | Single-call; repeat throws |
| Fallback to TaskContext? | No — fail fast if unconfigured |
| Test reset | Support reset via DestroyInstance() + re-Configure |

### Long-term evaluation: split template assets from runtime config

```csharp
// Template-only singleton (no config dependency)
AutoPickTemplateAssets : Singleton<AutoPickTemplateAssets>

// Runtime config object (explicitly constructed)
AutoPickRuntimeAssets(IAutoPickConfigProvider provider)
```

This separation aligns with broader migration — template matching assets are truly
singleton; key bindings and runtime behavior are per-context. But it's a larger
refactor than Phase B requires. The Configure() approach is sufficient for Phase B.

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

### Adapter Input — No Redundancy

**Avoid:** passing both `OtherConfig.Ocr` and `PaddleOcrModelConfig` — the former already contains the latter.

**Choose one:**
- **Option A (minimal):** `MacCoreRuntimeAdapter(AutoPickConfig, PaddleOcrModelConfig, string cultureInfo, IAutoPickRuntimeState)`
- **Option B (full config):** `MacCoreRuntimeAdapter(AutoPickConfig, OtherConfig.Ocr, IAutoPickRuntimeState)` — adapter maps to interface internally

**Recommendation:** Option A — adapter receives only what the three interfaces expose. No redundant parameters.

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

## 9. Revised Phase B Steps (B1-B6)

Each step must include:
- Consumer signature change (if any)
- Composition root / DI registration
- Windows provider implementation
- macOS adapter wiring (if applicable)
- Core/Verification test harness update
- Must compile and pass 15/15 at every step

### B1: Provider implementations + composition-root registration (no consumer changes)

**Scope:** Windows adapter + DI registration for all three Phase A interfaces.
Do NOT modify OcrFactory, AutoPickTrigger, or AutoPickAssets yet.

| File | Change |
|------|--------|
| `BetterGenshinImpact.Core/Adapters/WindowsAutoPickConfigProvider.cs` | New — reads from `TaskContext.Instance().Config.AutoPickConfig` |
| `BetterGenshinImpact.Core/Adapters/WindowsOcrRuntimeConfigProvider.cs` | New — reads from `TaskContext.Instance().Config.OtherConfig` |
| `BetterGenshinImpact.Core/Adapters/WindowsAutoPickRuntimeState.cs` | New — wraps `RunnerContext.Instance` |
| `BetterGenshinImpact/App.xaml.cs` | Register all three in DI container |
| `BetterGenshinImpact.Platform.Abstractions/` | No changes — interfaces live in `Core/Abstractions/Runtime/` |

**Verification:** `dotnet build BetterGenshinImpact.sln` — all providers exist, no functional change.
**macOS:** Not affected — B4 creates Mac adapters.

### B2: OcrFactory — inject IOcrRuntimeConfigProvider

- Add `IOcrRuntimeConfigProvider` parameter to `OcrFactory(ILogger, IOcrRuntimeConfigProvider)`
- Replace `GetConfig()` body with provider access
- Windows: DI resolves the registered `WindowsOcrRuntimeConfigProvider`
- macOS: DI or explicit construction uses `MacCoreRuntimeAdapter`
- **Must include in same commit:**
  - OcrFactory constructor change
  - Windows DI registration update (App.xaml.cs)
  - macOS adapter wiring (Verification test)
  - Default-config fallback for OcrFactory.Paddle static (still works via DI)

**Not changed in B2:**
- `OcrFactory.Paddle` static — still uses `App.ServiceProvider`
- `BgiOnnxFactory` service locator — still resolved via DI
- `PickTextInference` — unchanged

### B3: AutoPickTrigger — inject IAutoPickRuntimeState

- Add `IAutoPickRuntimeState` parameter to new constructor overload `AutoPickTrigger(IAutoPickRuntimeState)`
- Parameterless constructor keeps `RunnerContext.Instance` for backward compat
- Windows: uses parameterless constructor (no change)
- macOS: uses new overload with adapter-provided state
- Query: `state.StopCount` instead of `RunnerContext.Instance.AutoPickTriggerStopCount`

**Same commit must include:**
- AutoPickTrigger new overload
- macOS trigger creation path uses new overload
- Windows unchanged

### B4: AutoPickAssets — explicit Configure() (not static setter)

- Add `Configure(IAutoPickConfigProvider)` instance method
- Private constructor reads config only if already configured
- Fail fast if OnCapture called before Configure
- Unlike a static setter, this is NOT a static gateway — the provider reference is per-instance.
- Composition root calls it after first Instance access, before first business use.
- Windows: called after TaskContext init
- macOS: called after adapter creation

**Same commit must include:**
- AutoPickAssets.Configure()
- Composition root wiring (both platforms)
- Verification test setup
- AutoPickTrigger.OnCapture calls are protected (checks configured state)

### B5: Create MacCoreRuntimeAdapter + trigger factory

- `Core/Adapters/MacCoreRuntimeAdapter` — implements `IAutoPickConfigProvider` + `IOcrRuntimeConfigProvider`
- Input (no redundancy): `(AutoPickConfig, PaddleOcrModelConfig, string cultureInfo)`
- `Core/Adapters/MacAutoPickRuntimeState` — implements `IAutoPickRuntimeState`
- `Core/Adapters/MacTriggerFactory` — creates triggers with macOS-specific wiring
- Verification test uses MacCoreRuntimeAdapter for all three interfaces

### B6: Delete Shim Files (conditional)

Only possible when no linked file accesses `TaskContext.Instance()` or `RunnerContext.Instance()`.

| File | Direct Caller Count | Ready to Delete? |
|------|---------------------|------------------|
| `Shim/TaskContext.cs` | 5 active (AutoPickTrigger:2, AutoPickAssets:2, CaptureContent:1) | ❌ — B2+B4+B5 must finish first |
| `Shim/RunnerContext.cs` | 1 active (AutoPickTrigger:164) | ❌ — B3 must finish first |
| `Shim/CoreConfig.cs` | 0 (only referenced by Shim/TaskContext) | ⏳ — after B2+B4 |

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

| Step | Scope | Existing Prerequisite |
|------|-------|----------------------|
| B1 | Provider impl + DI registration (no consumer changes) | Phase A interfaces |
| B2 | OcrFactory ctor + IOcrRuntimeConfigProvider | B1 (Windows provider must exist) |
| B3 | AutoPickTrigger new overload + IAutoPickRuntimeState | B1 (Windows state provider must exist) |
| B4 | AutoPickAssets.Configure() | B1 (config provider must exist) |
| B5 | MacCoreRuntimeAdapter + trigger factory | B2-B4 (consumer changes must work first) |
| B6 | Delete Shim files | All above (direct caller count = 0) |

B1-B6 are **sequential**, not independent. B2 depends on B1 (Windows provider must be registered). B5 depends on B2-B4 (consumer changes must compile). B6 is gated on zero direct callers across all prior steps.
