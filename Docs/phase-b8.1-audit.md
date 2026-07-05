# B8.1 Audit: Input Send Extraction

**Status:** Audit only — no code changes
**Predecessor:** B7 complete (commit `0cf37a7`)

---

## 1. Input Call Sites in AutoPickTrigger.OnCapture()

Five call sites in `BetterGenshinImpact/GameTask/AutoPick/AutoPickTrigger.cs`:

| # | Line | Code | IInputBackend equivalent | Trigger condition |
|---|------|------|--------------------------|-------------------|
| 1 | 198 | `Simulation.SendInput.Mouse.VerticalScroll(2)` | `Scroll(2)` | Scroll-wheel icon detected |
| 2 | 210 | `Simulation.SendInput.Keyboard.KeyPress(PickVk)` | `KeyPress(pickVk)` | `ForceInteraction == true` |
| 3 | 257 | `Simulation.SendInput.Keyboard.KeyPress(PickVk)` | `KeyPress(pickVk)` | No list, no chat/settings icon |
| 4 | 355 | `Simulation.SendInput.Keyboard.KeyPress(PickVk)` | `KeyPress(pickVk)` | Whitelist match |
| 5 | 386 | `Simulation.SendInput.Keyboard.KeyPress(PickVk)` | `KeyPress(pickVk)` | All checks passed |

**Only two IInputBackend operations needed:** `KeyPress(BgiKey)` × 4 + `Scroll(int)` × 1.

**On Core/macOS,** these calls already route through the shim `SendInputFacade` → `PlatformServices.Input` (static gateway). The B8.1 goal is to eliminate `PlatformServices.Input` static access and inject `IInputBackend` explicitly.

---

## 2. Scroll Semantics Audit (Scroll(int delta))

**Interface definition (`IInputBackend.cs:40`):**
```csharp
void Scroll(int delta);
```

No documentation on the `delta` unit. Both call sites must agree on semantics.

**Call site semantics in AutoPickTrigger:**
`Simulation.SendInput.Mouse.VerticalScroll(2)` — Win32: `mouse_event(MOUSEEVENTF_WHEEL, ...)` with `dwData = 2 * WHEEL_DELTA = 2 * 120 = 240`. Result: 2 logical wheel notches upward (scroll content down).

**Current behavior:**
- **Windows (Fischless.InputSimulator):** `VerticalScroll(2)` sends 2 notches via Win32 `mouse_event(WHEEL_DELTA * 2)`.
- **Core shim (SendInputFacade):** `MouseFacade.VerticalScroll(2)` → `PlatformServices.Input.Scroll(2)`.
- **No macOS backend exists yet** — so `Scroll(2)` semantics are defined only by what the future macOS IInputBackend implements.

**Risk:** If `Scroll(2)` is interpreted as 2 pixels or 2 lines instead of 2 notches, scroll behavior differs. The macOS backend implementation must multiply by the platform scroll unit (e.g., 1 line = ~3 pixels on macOS, but item cycling depends on game UI layout, not OS units).

**Recommendation:** Document the scroll contract explicitly when creating the macOS backend: `Scroll(n)` means `n` logical scroll "clicks" — equivalent to `MOUSEEVENTF_WHEEL` with `WHEEL_DELTA * n` on Windows.

---

## 3. AutoPickTrigger Construction Points (all projects)

### Production paths

| # | File:line | Constructor | Context |
|---|-----------|-------------|---------|
| 1 | `GameTaskManager.cs:47` | `new AutoPickTrigger()` | Windows dispatcher startup |
| 2 | `GameTaskManager.cs:97` | `new AutoPickTrigger(externalConfig)` | Windows script launch |

### macOS composition

| # | File:line | Constructor | Context |
|---|-----------|-------------|---------|
| 3 | `MacAutoPickComposition.cs:52` | `new AutoPickTrigger(config, state, provider)` | macOS composition root |

### Verification (tests)

| # | File:line | Constructor | Context |
|---|-----------|-------------|---------|
| 4 | `Program.cs:184` | `new AutoPickTrigger(null, state)` | B5 StopCount test |
| 5 | `Program.cs:190` | `new AutoPickTrigger(null, state)` | B5 StopCount=2 test |
| 6 | `Program.cs:195` | `new AutoPickTrigger(null)` | B5 null config test |
| 7 | `Program.cs:205` | `new AutoPickTrigger(external)` | B5 externalConfig test |
| 8 | `Program.cs:213` | `new AutoPickTrigger(external, state)` | B5 combined test |
| 9 | `Program.cs:222` | `new AutoPickTrigger()` | B5 parameterless test |

No reflection/Activator-based construction found outside tests.

---

## 4. Windows Injection Chain

### 4.1 TaskTriggerDispatcher → GameTaskManager → AutoPickTrigger

```
TaskTriggerDispatcher.Start(hWnd, mode)
  ├─ GameCaptureFactory.Create(mode)
  ├─ TaskContext.Instance().Init(hWnd)                       // Windows HWND init
  ├─ AutoPickAssets.Instance.Configure(_autoPickConfigProvider) // B6 DI
  ├─ GameTaskManager.LoadInitialTriggers()                    // B8.1 target
  │   └─ new AutoPickTrigger()                                // parameterless ctor
  └─ GameCapture.Start(...)
```

`TaskTriggerDispatcher` **already receives `IAutoPickConfigProvider` via constructor** (line 59). It can **also receive `IInputBackend`** and pass it to `LoadInitialTriggers()`.

### 4.2 AddTrigger path

```
TaskTriggerDispatcher.AddTrigger(name, externalConfig)
  └─ GameTaskManager.AddTrigger(name, externalConfig)
      └─ new AutoPickTrigger(externalConfig)
```

`TaskTriggerDispatcher.AddTrigger` is called from script execution paths. The dispatcher holds the `IInputBackend` reference and can forward it.

### 4.3 Other LoadInitialTriggers callers

| File:line | Path |
|-----------|------|
| `TaskRunner.cs:190` | `TaskTriggerDispatcher.Instance().SetTriggers(GameTaskManager.LoadInitialTriggers())` |
| `ScriptService.cs:432` | **Commented out** — not active |

Only `TaskTriggerDispatcher.Start()` and `TaskRunner` call `LoadInitialTriggers()`. `TaskRunner` does so via `TaskTriggerDispatcher.Instance()`, so the dispatcher singleton is the owner.

### 4.4 GameTaskManager is static — no constructor injection

`GameTaskManager` is `internal class` with all static methods. It cannot receive DI via constructor. The injection must flow through method parameters:

**Path A (recommended): static method parameters**

```csharp
// GameTaskManager
public static List<ITaskTrigger> LoadInitialTriggers(IInputBackend inputBackend) { ... }
public static bool AddTrigger(string name, object? config, IInputBackend inputBackend) { ... }

// TaskTriggerDispatcher.Start()
_triggers = GameTaskManager.LoadInitialTriggers(_inputBackend);

// TaskTriggerDispatcher.AddTrigger()
GameTaskManager.AddTrigger(name, externalConfig, _inputBackend);
```

This matches the existing static-call pattern and minimizes refactoring scope. `TaskTriggerDispatcher` already holds injected dependencies — adding `IInputBackend` is a one-line constructor change.

**Path B: make GameTaskManager an instance service (rejected for B8.1)**
Would require DI registration, rewriting all static callers, and touching non-AutoPick trigger types. Not in B8.1 scope.

### 4.5 WPF IInputBackend Gap

The WPF project currently has **no `IInputBackend` implementation** and no reference to `BetterGenshinImpact.Platform.Abstractions`. `DesktopRegion.cs` uses `PlatformServices.Input` but:
- `PlatformServices` doesn't exist in the WPF project
- The WPF project hasn't been compiled since the cross-platform porting work began

B8.1 will need to:
1. Add `BetterGenshinImpact.Platform.Abstractions` reference to WPF csproj
2. Create `Win32InputBackend : IInputBackend` wrapping Fischless.WindowsInput
3. Register `IInputBackend` in WPF DI (`App.xaml.cs`)
4. Inject into `TaskTriggerDispatcher` constructor

This is a prerequisite for B8.1 — the WPF backend must exist before AutoPickTrigger can require `IInputBackend`.

---

## 5. Migration Strategy: Option B (Required Injection)

### Master constructor — non-nullable IInputBackend

```csharp
public AutoPickTrigger(
    AutoPickExternalConfig? config,
    IAutoPickRuntimeState? runtimeState,
    IAutoPickConfigProvider? configProvider,
    IInputBackend inputBackend)
{
    ArgumentNullException.ThrowIfNull(inputBackend);
    _inputBackend = inputBackend;
    ...
}
```

No `PlatformServices.Input` fallback. No nullable parameter. No static gateway.

### Old overload disposition

| Overload | Disposition |
|----------|------------|
| `new AutoPickTrigger()` | **Delete.** No backend → can't function. |
| `new AutoPickTrigger(config)` | **Delete.** Same. |
| `new AutoPickTrigger(config, state)` | **Delete.** Same. |
| `new AutoPickTrigger(config, state, provider)` | **Add IInputBackend parameter.** |

All callers that previously used deleted overloads must be updated to pass IInputBackend explicitly.

### Caller updates

| Caller | Change |
|--------|--------|
| `GameTaskManager.LoadInitialTriggers()` | Accept IInputBackend param; pass to new AutoPickTrigger |
| `GameTaskManager.AddTrigger()` | Accept IInputBackend param; pass to AutoPickTrigger (also AutoPick external) |
| `TaskTriggerDispatcher` | Receive IInputBackend in constructor; pass to GameTaskManager methods |
| `TaskRunner.cs:190` | Pass IInputBackend via dispatcher (already holds it) |
| `MacAutoPickComposition.Compose()` | Pass IInputBackend (trivial — one more param) |
| Verification tests | Pass `RecordingInputBackend` |

### WPF prerequisites (before B8.1 implementation)

1. Create `BetterGenshinImpact/Core/Runtime/Windows/Win32InputBackend.cs` implementing `IInputBackend`
2. Register `IInputBackend` → `Win32InputBackend` in `App.xaml.cs` DI
3. Inject into `TaskTriggerDispatcher(IAutoPickConfigProvider, IInputBackend)`
4. Fix `DesktopRegion.cs` (uses `PlatformServices.Input` which doesn't exist in WPF) — either add `#if` guard or switch to injected `IInputBackend`
5. Add `BetterGenshinImpact.Platform.Abstractions` project reference to WPF csproj

---

## 6. Migration Steps (B8.1 implementation)

| Step | Description | Files |
|------|-------------|-------|
| B8.1.0 | Create Win32InputBackend + register in WPF DI | New file + App.xaml.cs + WPF csproj |
| B8.1.1 | Add IInputBackend to TaskTriggerDispatcher constructor | TaskTriggerDispatcher.cs + App.xaml.cs |
| B8.1.2 | GameTaskManager.LoadInitialTriggers accepts IInputBackend | GameTaskManager.cs |
| B8.1.3 | GameTaskManager.AddTrigger accepts IInputBackend | GameTaskManager.cs |
| B8.1.4 | AutoPickTrigger constructor: add required IInputBackend, delete old overloads | AutoPickTrigger.cs |
| B8.1.5 | Replace Simulation.SendInput calls with `_inputBackend.KeyPress/Scroll` | AutoPickTrigger.cs (5 sites) |
| B8.1.6 | MacAutoPickComposition passes IInputBackend | MacAutoPickComposition.cs |
| B8.1.7 | Fix DesktopRegion.cs for WPF (use injected backend or #if guard) | DesktopRegion.cs |
| B8.1.8 | Update verification tests: all new AutoPickTrigger calls pass backend | Program.cs |
| B8.1.9 | Update TaskRunner.cs call site | TaskRunner.cs |
| B8.1.10 | Build + verification | All projects |

**Do NOT:**
- Create `IAutoPickInput` or wrapper interfaces
- Change `IInputBackend` API
- Keep static fallback `_inputBackend ?? PlatformServices.Input`
- Touch B8.2/B8.3/OCR/Yap
- Delete Shim files
