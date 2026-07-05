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

### Recommended: Configure() — But Constructor Must Be Split

The current private constructor reads config immediately:
```csharp
private AutoPickAssets()
{
    var keyName = TaskContext.Instance().Config.AutoPickConfig.PickKey;
    // LoadCustomPickKey, PickVk mapping, ChatPickRo ...
}
```

This means `Configure()` called AFTER `Instance` first access is **too late**. The
constructor has already read `TaskContext.Instance()` before `Configure()` runs.

**Required refactoring:**

1. Private constructor initializes only config-independent template assets:
```csharp
private AutoPickAssets()
{
    FRo = new RecognitionObject { ... template only, no config ... };
    ChatIconRo = new RecognitionObject { ... };
    SettingsIconRo = new RecognitionObject { ... };
    LRo = new RecognitionObject { ... };
    // NO PickKey, NO PickVk, NO ChatPickRo here
}
```

2. All config-dependent initialization moves to `Configure()`:
```csharp
public void Configure(IAutoPickConfigProvider provider)
{
    if (_configured)
        throw new InvalidOperationException("AutoPickAssets is already configured.");
    _configProvider = provider;
    var keyName = provider.AutoPickConfig.PickKey;
    PickRo = LoadCustomPickKey(keyName);
    PickVk = BgiKeyMapper.ToKey(keyName);
    ChatPickRo = LoadCustomChatPickKey(keyName);
    _configured = true;
}
```

3. `PickVk` and `PickRo` remain mutable (fields, not readonly) since they are
   now set in `Configure()`, not the constructor.

| Property | Value |
|----------|-------|
| Invoked by | Composition root, immediately after `AutoPickAssets.Instance` first access |
| Must complete before | First access to `PickRo`/`PickVk` by any caller (AutoPickTrigger.Init, BvSimpleOperation, etc.) |

Fail fast if config-dependent fields (`PickRo`, `PickVk`) accessed before `Configure`. AutoPickTrigger.Init() or any caller must `EnsureConfigured()` first.

### B7: macOS trigger factory / composition root

- `MacTriggerFactory` creates triggers with macOS wiring
- macOS bootstrap calls factory, then dispatcher
- Windows unchanged

### B8: Delete Shim files

**Gate:** Direct caller count must reach zero. Verify with:

```bash
rg 'TaskContext\.Instance\(' BetterGenshinImpact/GameTask/AutoPick/ BetterGenshinImpact/GameTask/CaptureContent.cs BetterGenshinImpact/GameTask/Model/BaseAssets.cs BetterGenshinImpact/Core/Recognition/OCR/OcrFactory.cs
rg 'RunnerContext\.Instance' BetterGenshinImpact/GameTask/AutoPick/
rg -l 'Shim/TaskContext\|Shim/RunnerContext\|Shim/CoreConfig' BetterGenshinImpact.Core/BetterGenshinImpact.Core.csproj
```

Delete only when all three searches return zero results in the linked-file set.

| File | Condition to Delete |
|------|---------------------|
| `Shim/TaskContext.cs` | Zero linked files access `TaskContext.Instance()` for AutoPick/OCR config |
| `Shim/RunnerContext.cs` | Zero linked files access `RunnerContext.Instance` |
| `Shim/CoreConfig.cs` | Shim/TaskContext CSproj reference removed |

---

## Summary: Phase B Feasibility

| Step | Scope | Dependency |
|------|-------|------------|
| B1 | Interface file relocation — no consumer changes | None (Phase A complete) |
| B2 | Windows providers + DI — no consumer changes | B1 (interfaces must be visible) |
| B3 | macOS providers + Verification wiring — no consumer changes | B1 |
| B4 | OcrFactory ctor + provider injection | B2+B3 (both providers must exist) |
| B5 | AutoPickTrigger overload + runtime-state injection | B3 (mac state must exist) |
| B6 | AutoPickAssets constructor split + Configure() | B3 (mac config provider must exist) |
| B7 | macOS trigger factory | B4-B6 (consumers must work) |
| B8 | Delete Shim files | All above (caller count = 0) |

B1-B3 are sequential: B2 and B3 each depend on B1 (interfaces must be visible first). B4-B6 depend on B2+B3. B7 depends on B4-B6. B8 is terminal.

---

## 9. Service Locator Cleanup — Deferred to Phase C

These are NOT addressed in Phase B:
- `OcrFactory.Paddle` static (20+ call sites in Windows host)
- `PickTextInference` constructor (uses `App.ServiceProvider.GetRequiredService<BgiOnnxFactory>()`)

Phase C will:
- Replace `OcrFactory.Paddle` static with injected `IOcrService`
- Inject `BgiOnnxFactory` into `PickTextInference`
- Remove `App.ServiceProvider` calls from recognition code

Phase B's goal is to **add config providers** — not eliminate all service locators.

| Step | Scope | Existing Prerequisite |
|------|-------|----------------------|
| B1 | Provider impl + DI registration (no consumer changes) | Phase A interfaces |
| B2 | OcrFactory ctor + IOcrRuntimeConfigProvider | B1 (Windows provider must exist) |
| B3 | AutoPickTrigger new overload + IAutoPickRuntimeState | B1 (Windows state provider must exist) |
| B4 | AutoPickAssets.Configure() | B1 (config provider must exist) |
| B5 | MacCoreRuntimeAdapter + trigger factory | B2-B4 (consumer changes must work first) |
| B6 | Delete Shim files | All above (direct caller count = 0) |

B1-B6 are **sequential**, not independent. B2 depends on B1 (Windows provider must be registered). B5 depends on B2-B4 (consumer changes must compile). B6 is gated on zero direct callers across all prior steps.
