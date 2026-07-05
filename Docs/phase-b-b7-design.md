# B7 Design: macOS Composition Root

**Phase:** B7 (macOS trigger composition root)  
**Predecessor:** B6 series (constructor split + Configure lifecycle)  
**Status:** Design review — DO NOT implement until approved

---

## 1. TaskContext/Win32 Dependency Audit in AutoPickTrigger

Before designing the composition root, we must verify that the trigger-creation call chain
does not silently re-enter `TaskContext.Instance()` or `RunnerContext.Instance` through
any of the objects the composition root creates.

### 1.1 Init() Dependencies (lines 84–107)

| Line | Code | Dependency | Resolved via |
|------|------|-----------|-------------|
| 88 | `TaskContext.Instance().Config.AutoPickConfig` | `config.Enabled` | IAutoPickConfigProvider |
| 88 | (same) | `config.BlackListEnabled` | IAutoPickConfigProvider |
| 88 | (same) | `config.WhiteListEnabled` | IAutoPickConfigProvider |
| 93 | `ReadJson(@"Assets\Config\Pick\default_pick_black_lists.json")` | `Global.ReadAllTextIfExist` + `ConfigService.JsonOptions` | Shim — Phase C |
| 94 | `ReadText(@"User\pick_black_lists.txt")` | `Global.ReadAllTextIfExist` | Shim — Phase C |
| 100 | `ReadTextList(@"User\pick_fuzzy_black_lists.txt")` | `Global.ReadAllTextIfExist` | Shim — Phase C |
| 105 | `ReadText(@"User\pick_white_lists.txt")` | `Global.ReadAllTextIfExist` | Shim — Phase C |
| 122 | `ThemedMessageBox.Error(...)` | UI (WPF) — shim in Core | Shim — Phase C |

**Conclusion for Init():** Exactly **one** `TaskContext.Instance()` call (line 88), reading `AutoPickConfig`.  
All three fields (`Enabled`, `BlackListEnabled`, `WhiteListEnabled`) are already available via `IAutoPickConfigProvider.AutoPickConfig`.

### 1.2 StopCount Property (lines 61–62)

```csharp
private int StopCount =>
    _runtimeState?.StopCount ?? RunnerContext.Instance.AutoPickTriggerStopCount;
```

This falls back to `RunnerContext` when `_runtimeState` is null (Windows parameterless path).  
When the macOS composition root passes a non-null `IAutoPickRuntimeState`, `RunnerContext.Instance` is **never** accessed.

**No change needed** — the guard already works. Verification must assert the macOS path reaches `_runtimeState.StopCount`, not the RunnerContext fallback.

### 1.3 OnCapture() Dependencies (lines 181–389)

| Line | Code | Dependency | B7 scope |
|------|------|-----------|----------|
| 198 | `Simulation.SendInput.Mouse.VerticalScroll(2)` | Win32 SendInput | Out of scope — needs platform input abstraction for scroll |
| 210, 257, 355, 386 | `Simulation.SendInput.Keyboard.KeyPress(AutoPickAssets.Instance.PickVk)` | Win32 SendInput | Out of scope — same as above |
| 214 | `TaskContext.Instance().SystemInfo.AssetScale` | Win32 window metrics | Out of scope — needs ISystemInfoProvider |
| 215 | `TaskContext.Instance().Config.AutoPickConfig` | Config offsets, OCR engine, list enabled flags | Out of scope (B8+) |
| 297 | `TextInferenceFactory.Pick.Value.Inference(...)` | Static Yap engine factory | Out of scope — ONNX static gateway |
| 310, 331 | `OcrFactory.Paddle.Ocr*(...)` | Static OCR gateway | Out of scope — already injected but statically accessed |

**OnCapture() is out of B7 scope.** The composition root creates a trigger but does not
invoke the capture loop. The Swift host is responsible for calling `OnCapture()` only after
it has set up `PlatformServices.Input`, `DesktopRegion` dimensions, and (eventually) a non-shim
`TaskContext.Instance().SystemInfo`.

### 1.4 Required Narrow Change for Init()

Make `IAutoPickConfigProvider` injectable into `AutoPickTrigger` so Init() can read
`AutoPickConfig` fields from the provider instead of `TaskContext.Instance()`:

**New field + constructor parameter (AutoPickTrigger.cs):**
```csharp
private readonly IAutoPickConfigProvider? _configProvider;

public AutoPickTrigger(
    AutoPickExternalConfig? config,
    IAutoPickRuntimeState? runtimeState,
    IAutoPickConfigProvider? configProvider = null)
{
    _autoPickAssets = AutoPickAssets.Instance;
    _externalConfig = config;
    _runtimeState = runtimeState;
    _configProvider = configProvider;
}
```

**Updated Init() (line 88 only):**
```csharp
var config = _configProvider?.AutoPickConfig
             ?? TaskContext.Instance().Config.AutoPickConfig;
```

- Windows callers (parameterless ctor / config-only ctor): `_configProvider` is null → falls back to `TaskContext.Instance().Config.AutoPickConfig` — **zero breakage**.
- macOS composition root: passes `IAutoPickConfigProvider` → `TaskContext.Instance()` is **never accessed** during Init().
- `OnCapture()` line 215 (`TaskContext.Instance().Config.AutoPickConfig`) is NOT changed — this is a separate follow-up (B8+).

This is a minimal, backward-compatible, verifiable change. The macOS creation path (`new AutoPickTrigger(null, runtimeState, configProvider)`) becomes TaskContext-free for Init().

---

## 2. Composition Root Design

### 2.1 Location and Naming

**File:** `BetterGenshinImpact.Core/Composition/MacAutoPickComposition.cs`

**Not** in `Core/Adapters/`. Adapters contain interface implementations (MacCoreRuntimeAdapter, MacAutoPickRuntimeState). The composition root **creates and wires** those adapters — it is the assembler, not the assembled.

**Alternative considered:** `Core/Bootstrap/MacAutoPickComposition.cs`. Both are acceptable. `Composition/` is preferred because it keeps all composition types in one directory for future expansion (e.g., a combined `MacComposition` for all trigger types).

**Class name:** `MacAutoPickComposition` (not `MacTriggerFactory`). "Factory" implies a generic creation pattern; `Composition` accurately describes the responsibility: assembling the full object graph for one trigger type.

### 2.2 Interface

```csharp
namespace BetterGenshinImpact.Core.Composition;

public sealed class MacAutoPickComposition : IDisposable
{
    private static bool _composed;

    public AutoPickTrigger Trigger { get; }

    private MacAutoPickComposition(AutoPickTrigger trigger)
    {
        Trigger = trigger;
    }

    /// <summary>
    /// Compose a fully-wired AutoPickTrigger for macOS.
    /// Call exactly once per process lifetime.
    /// </summary>
    /// <param name="configProvider">AutoPick + OCR config provider (MacCoreRuntimeAdapter or custom).</param>
    /// <param name="runtimeState">AutoPick runtime state (StopCount coordination).</param>
    /// <param name="externalConfig">Optional script-layer override config.</param>
    public static MacAutoPickComposition Compose(
        IAutoPickConfigProvider configProvider,
        IAutoPickRuntimeState runtimeState,
        AutoPickExternalConfig? externalConfig = null)
    {
        if (_composed)
            throw new InvalidOperationException(
                "MacAutoPickComposition has already been composed. " +
                "Only one composition is allowed per process lifetime. " +
                "Restart the application to re-initialize.");

        AutoPickAssets.Instance.Configure(configProvider);

        var trigger = new AutoPickTrigger(externalConfig, runtimeState, configProvider);
        trigger.Init();

        _composed = true;
        return new MacAutoPickComposition(trigger);
    }

    /// <summary>
    /// For verification tests only. Resets composition state so tests
    /// can run Compose() multiple times in a single process.
    /// </summary>
    internal static void ResetForVerification()
    {
        _composed = false;
        AutoPickAssets.DestroyInstance();
    }

    public void Dispose()
    {
        // Future: unregister dispatcher hooks, stop capture loop, etc.
    }
}
```

### 2.3 Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Parameters vs options record | Narrow constructor parameters (`IAutoPickConfigProvider`, `IAutoPickRuntimeState`, `AutoPickExternalConfig?`) | No `MacRuntimeOptions` monolith. The composition root receives already-constructed dependencies; the macOS host creates adapters. |
| `StopCount` as parameter | **Not** a parameter — comes from `IAutoPickRuntimeState.StopCount` | The int snapshot in the old `MacRuntimeOptions` design was premature. The interface reference is dynamic; future mutable implementations propagate changes automatically. |
| One compose per process | `_composed` static guard with `InvalidOperationException` | Prevents double-configuration of `AutoPickAssets` singleton and dual-trigger states. Matches AutoPickAssets.Configure() single-call semantics. |
| Multiple triggers | One trigger per composition | `AutoPickAssets` is a singleton with single Configure() — creating multiple triggers sharing the same assets is valid but not currently useful. The composition exposes exactly one trigger for the current dispatcher lifecycle. |
| `ResetComposition()` public API | **Deleted.** Replaced with `internal ResetForVerification()`. | Public reset enables the dangerous dual-instance state described in B6 testing. At the process level, restart is the only safe reset path. |
| `IDisposable` | Composition implements `IDisposable` | Future-proof for dispatcher unregistration, capture loop teardown, etc. Currently a no-op dispose. |

### 2.4 Why Not a Generic Factory

- `AutoPickAssets` is a singleton with single-call `Configure()` — the composition is intrinsically one-shot.
- The trigger-creation logic is simple (`new AutoPickTrigger(...)` + `Init()`) — a separate factory class adds indirection without abstraction.
- Adding other trigger types (AutoSkip, AutoFight) would each need their own composition root or a combined one; a generic factory pattern would constrain the initialization order unnecessarily.

---

## 3. Full macOS Creation Path (post-B7)

```
Swift host / .NET bridge
  │
  ├─ PlatformServices.Input = MacCGEventBackend            (B3-style)
  ├─ DesktopRegion.DisplayWidth/Height = screen metrics     (B3-style)
  ├─ TaskContext.Instance().SystemInfo = MacSystemInfo()    (shim)
  │
  ├─ var adapter = new MacCoreRuntimeAdapter(
  │       config, PaddleOcrModelConfig.V5, "zh-Hans")
  ├─ var state = new MacAutoPickRuntimeState(0)
  │
  └─ var composition = MacAutoPickComposition.Compose(
         adapter, state, externalConfig: null)
       ├─ AutoPickAssets.Instance.Configure(adapter)        ← TaskContext-free (guarded in Core)
       ├─ new AutoPickTrigger(null, state, adapter)         ← IAutoPickConfigProvider injected
       └─ trigger.Init()                                    ← reads adapter.AutoPickConfig (NOT TaskContext)
```

**Init() TaskContext accesses:** **Zero** (verified by rg search + assertion).

**OnCapture() TaskContext accesses:** Still present (AssetScale + AutoPickConfig offsets). These are out of B7 scope. The Swift host is not yet running the capture loop.

---

## 4. Lifecycle

| Step | Actor | When | Constraint |
|------|-------|------|------------|
| Pre-condition | macOS host | Before `Compose()` | `PlatformServices.Input` set, `DesktopRegion` dimensions set, `TaskContext.Instance().SystemInfo` set (shim) |
| Compose | `MacAutoPickComposition.Compose(...)` | Once per process | `_composed` guard; throws on second call |
| Assets configured | `AutoPickAssets.Configure(provider)` | Inside `Compose()` | Must be called exactly once, throws on duplicate |
| Trigger created | `new AutoPickTrigger(...)` | Inside `Compose()` | Receives all three injected dependencies (config, runtime, provider) |
| Init called | `trigger.Init()` | Inside `Compose()` | Blacklists/whitelists loaded from disk (shim Global); IsEnabled set from provider |
| Trigger ready | `composition.Trigger` | After `Compose()` returns | Dispatcher can begin tick loop |
| Shutdown | `composition.Dispose()` | Process exit | Currently no-op; future-proof |
| Reset | `MacAutoPickComposition.ResetForVerification()` | **Tests only** | `internal` — NOT accessible to production callers |

---

## 5. Verification Plan

### 5.1 New Tests (added to `Program.cs`)

| # | Test | Assertion |
|---|------|-----------|
| B7.1 | `MacAutoPickComposition.ResetForVerification()` | `internal` — compiles from test project via `InternalsVisibleTo` (add to csproj) |
| B7.2 | Compose with provider + state (no external config) | `composition.Trigger` is non-null |
| B7.3 | Compose preserves external config reference | `_externalConfig` field == original object (reflection) |
| B7.4 | Compose preserves runtime state reference | `_runtimeState` field == original object (reflection) |
| B7.5 | Init() reads IsEnabled from provider | `trigger.IsEnabled` == `provider.AutoPickConfig.Enabled` |
| B7.6 | Double Compose throws | `InvalidOperationException` with specific message |
| B7.7 | After ResetForVerification, Compose succeeds again | Reconfigured trigger works |
| B7.8 | Init() call chain: zero TaskContext.Instance() accesses | This can be verified by: (a) `rg 'TaskContext'` on `MacAutoPickComposition.cs` returns zero; (b) Init() receives `_configProvider` != null so the fallback `TaskContext.Instance().Config.AutoPickConfig` is NEVER reached |
| B7.9 | Compose with `enabled = false` | `trigger.IsEnabled == false` |
| B7.10 | Compose with `BlackListEnabled = false` | Init() skips blacklist loading (reflection on `_blackList` is empty) |
| B7.11 | Compose with `WhiteListEnabled = false` | Init() skips whitelist loading |
| B7.12 | Compose with `BlackListEnabled = true` | Init() loads default blacklist from assets JSON |
| B7.13 | `MacAutoPickComposition` is `IDisposable` | `using` statement compiles; `Dispose()` does not throw |

### 5.2 Existing Tests That Must Continue to Pass

- All 57 assertions from B1–B6 (`AutoPickAssets.Configure` lifecycle, `OcrFactory` injection, `AutoPickTrigger` constructor chain, `StopCount`, property guards)
- After `ResetForVerification()` + re-Compose in B7 tests, final cleanup must restore `AutoPickAssets` singleton to configured state so no downstream assertion fails

---

## 6. Implementation Plan

| Step | Files | Description |
|------|-------|-------------|
| 7.1 | `AutoPickTrigger.cs` | Add `_configProvider` field + master ctor parameter. Update `Init()` line 88 to prefer provider over `TaskContext`. Zero breakage for Windows (parameterless ctor leaves `_configProvider = null`). |
| 7.2 | `Core/Composition/MacAutoPickComposition.cs` | New file. `Compose()`, `ResetForVerification()`, `IDisposable`. |
| 7.3 | `BetterGenshinImpact.Core.csproj` | Add `<Compile Include="Composition/MacAutoPickComposition.cs" />` |
| 7.4 | `Test/.../BetterGenshinImpact.Core.Verification.csproj` | Add `InternalsVisibleTo` for verification project (or test accesses internal via assembly attribute) |
| 7.5 | `Test/.../Program.cs` | Add B7.1–B7.13 test assertions |
| 7.6 | Build + verify | `dotnet build` zero errors; `dotnet run` all tests pass |
| 7.7 | rg check | `rg 'TaskContext|RunnerContext' BetterGenshinImpact.Core/Composition/MacAutoPickComposition.cs` → zero hits |

### 7.1 Detail: AutoPickTrigger.cs Changes

**Constructor (replace master constructor at line 76):**
```csharp
public AutoPickTrigger(
    AutoPickExternalConfig? config,
    IAutoPickRuntimeState? runtimeState,
    IAutoPickConfigProvider? configProvider = null)
{
    _autoPickAssets = AutoPickAssets.Instance;
    _externalConfig = config;
    _runtimeState = runtimeState;
    _configProvider = configProvider;
}
```

**Init() change (replace line 88):**
```csharp
var config = _configProvider?.AutoPickConfig
             ?? TaskContext.Instance().Config.AutoPickConfig;
```

**No other changes to AutoPickTrigger.cs.** OnCapture() is unchanged for B7.

---

## 7. Out of Scope (Phase C / B8+)

| Item | Status | Resolution |
|------|--------|------------|
| OnCapture() TaskContext reads | Present (line 214–215) | Needs ISystemInfoProvider + broader config migration (B8) |
| Simulation.SendInput | Win32 | IInputBackend already exists; OnCapture needs migration (B8) |
| OcrFactory static gateway | Static singleton | Needs DI container or composition-scoped factory (C) |
| TextInferenceFactory static gateway | Static singleton | Same as OcrFactory (C) |
| Shim deletion | 17 files remain | Gated on zero direct callers (C) |
| Global file I/O abstraction | Shim in Core | Needs IFileSystem / IAssetPath (C) |
| ThemedMessageBox | Shim in Core | Needs IUserInteractionService (C) |
| Multiple trigger types | Only AutoPick | MacAutoPickComposition is single-purpose; future MacComposition could compose all types |
