# Porting Audit: TaskContext & RunnerContext

**Branch:** `mac-core-extraction` (commit `374a023`)
**Date:** 2026-07-05
**Status:** Audit only — no code changes made

---

## 1. Upstream Source Files

| Type | Path | Lines | Platform |
|------|------|-------|----------|
| TaskContext | `BetterGenshinImpact/GameTask/TaskContext.cs` | 111 | Windows host |
| RunnerContext | `BetterGenshinImpact/GameTask/RunnerContext.cs` | 217 | Windows host |

---

## 2. TaskContext — Full Field & Method Audit

### 2.1 Fields & Properties

| # | Member | Type | Windows Dependency | Classification |
|---|--------|------|--------------------|----------------|
| 1 | `Instance()` | singleton (LazyInitializer) | None | ✅ Pure C# — keep |
| 2 | `IsInitialized` | `bool` | None | ✅ Pure C# — keep |
| 3 | `GameHandle` | `IntPtr` | **Win32 HWND** | ❌ Remove from Core — macOS uses window ID |
| 4 | `PostMessageSimulator` | `PostMessageSimulator` | **Win32 PostMessage** | ❌ Remove — no macOS equivalent (keep #if guarded call site) |
| 5 | `DpiScale` | `float` | **Win32 DPI** | ❌ Remove — redundant; use `ISystemInfo.ScaleTo1080PRatio` |
| 6 | `SystemInfo` | `ISystemInfo` | Interface already cross-platform | ✅ Keep (via `ISystemInfo`, implemented by `MacSystemInfo`) |
| 7 | `Config` | `AllConfig` (all task configs) | References 20+ task config classes | ⚠️ Split — Core needs accessor, not full AllConfig |
| 8 | `LinkedStartGenshinTime` | `DateTime` | None | ✅ Pure C# — keep |
| 9 | `CurrentScriptProject` | `ScriptGroupProject` | ClearScript V8 | ⚠️ Defer — scripting not in Core scope yet |

### 2.2 Methods

| # | Method | Windows Dependency | Classification |
|---|--------|--------------------|----------------|
| 1 | `Init(IntPtr hWnd)` | **Win32 HWND** | ❌ Overload with `Init(GameWindowMetrics)` for macOS |
| 2 | `GetGenshinGameProcessNameList()` | None (pure string list) | ✅ Keep — uses `SystemInfo.GameProcessName` |
| 3 | `DestroyInstance()` | None (exists only in Shim) | ✅ Shim addition — keep for task lifecycle reset |

### 2.3 All Call Sites (in linked Core files)

| File:Line | Access | Guarded? |
|-----------|--------|----------|
| `AutoPickTrigger.cs:69` | `Config.AutoPickConfig` | No |
| `AutoPickTrigger.cs:195` | `SystemInfo.AssetScale` | No |
| `AutoPickTrigger.cs:196` | `Config.AutoPickConfig` | No |
| `AutoPickAssets.cs:70` | `Config.AutoPickConfig.PickKey` | No |
| `AutoPickAssets.cs:78` | `Config.KeyBindingsConfig` | ✅ `#if BGI_FULL_WINDOWS` |
| `AutoPickAssets.cs:86` | `Config.AutoPickConfig.PickKey` | No |
| `CaptureContent.cs:28` | `SystemInfo` | No |
| `BaseAssets.cs:21` | `SystemInfo` (constructor) | No |
| `Region.cs:99` | `PostMessageSimulator` | ✅ `#if BGI_FULL_WINDOWS` |
| `GameCaptureRegion.cs:29,46` | `DpiScale` | ✅ `#if BGI_FULL_WINDOWS` |
| `GameCaptureRegion.cs:94-111` | `SystemInfo.CaptureAreaRect` | ✅ `#if BGI_FULL_WINDOWS` |
| `OcrFactory.cs:59,78` | `Config.OtherConfig` | No — uses OtherConfig |
| `BgiOnnxFactory.cs:75` | `Config.HardwareAccelerationConfig` | BgiOnnxFactory is Shim'ed — N/A |

**Active (unguarded) call sites: 8** — all access either `SystemInfo` or `Config.AutoPickConfig`.
**Guarded call sites: 5** — all Windows-specific (PostMessage, DPI, capture rect in drawing methods).

---

## 3. RunnerContext — Full Field & Method Audit

### 3.1 Fields & Properties

| # | Member | Type | Cross-Platform? |
|---|--------|------|-----------------|
| 1 | `Instance` | Singleton (via `Singleton<T>`) | ✅ Pure C# |
| 2 | `IsContinuousRunGroup` | `bool` | ✅ State flag |
| 3 | `taskProgress` | `TaskProgress.TaskProgress` | ❌ Type not linked |
| 4 | `IsSuspend` | `bool` | ✅ State flag |
| 5 | `IsPreExecution` | `bool` | ✅ State flag |
| 6 | `SuspendableDictionary` | `Dictionary<string, ISuspendable>` | ❌ `ISuspendable` not linked (AutoPathing) |
| 7 | `isAutoFetchDispatch` | `bool` | ✅ State flag |
| 8 | `PartyName` | `string?` | ✅ State flag |
| 9 | `AutoPickTriggerStopCount` | `int` | ✅ **Used by AutoPickTrigger (1 call site)** |
| 10 | `_combatScenes` (private) | `CombatScenes?` | ❌ Not linked (AutoFight.Model) |

### 3.2 Methods

| # | Method | Lines | Core Relevance |
|---|--------|-------|---------------|
| 1 | `GetCombatScenes()` | 64-83 | ❌ AutoFight only — not in Core |
| 2 | `TrySyncCombatScenesSilent()` | 88-105 | ❌ AutoFight only — not in Core |
| 3 | `ClearCombatScenes()` | 107-110 | ❌ Not in Core |
| 4 | `Clear()` | 115-127 | ⚠️ Task lifecycle — defer |
| 5 | `Reset()` | 132-143 | ⚠️ Task lifecycle — defer |
| 6 | `StopAutoPick(int)` | 148-153 | ✅ **Needed for AutoPick coordination** |
| 7 | `ResumeAutoPick(int)` | 157-194 | ✅ **Needed for AutoPick coordination** |
| 8 | `StopAutoPickRunTask(Func<Task>, int)` | 200-212 | ✅ **Needed for AutoPick coordination** |
| 9 | `stop()` | 213-216 | ❌ AutoFight only |

### 3.3 All Call Sites (in linked Core files)

| File:Line | Access | Notes |
|-----------|--------|-------|
| `AutoPickTrigger.cs:164` | `AutoPickTriggerStopCount` | Only Core call site |

---

## 4. Config Access Pattern

Config is accessed via `TaskContext.Instance().Config.*` with these sub-properties:

| Config Property | Accessed From | File |
|----------------|---------------|------|
| `AutoPickConfig` | AutoPickTrigger, AutoPickAssets | 2 files, 4 calls |
| `AutoPickConfig.PickKey` | AutoPickAssets | 1 file, 2 calls |
| `KeyBindingsConfig.PickUpOrInteract` | AutoPickAssets | 1 call (guarded) |
| `OtherConfig.OcrConfig` | OcrFactory | 1 call |
| `OtherConfig.GameCultureInfoName` | OcrFactory | 1 call |
| `HardwareAccelerationConfig` | BgiOnnxFactory (shim'ed) | N/A |

---

## 5. Shim vs Upstream — Line-by-Line Delta

### TaskContext

| Feature | Upstream (111 lines) | Shim (66 lines) | Delta |
|---------|---------------------|-----------------|-------|
| Singleton pattern | `LazyInitializer` | Manual lock | Different impl |
| `Config` return type | `AllConfig` (all tasks) | `CoreConfig` (AutoPick + OtherConfig only) | **Incomplete** |
| `Init()` signature | `Init(IntPtr hWnd)` | `Init(GameWindowMetrics)` | Platform-agnostic — correct |
| `GameHandle` | Exposed | Missing | Intentionally omitted |
| `PostMessageSimulator` | Exposed | Missing | Windows-only, guarded call sites |
| `DpiScale` | Exposed | Missing | Redundant with `ISystemInfo.ScaleTo1080PRatio` |
| `GetGenshinGameProcessNameList()` | Full impl | Missing | Can be ported as-is |
| `DestroyInstance()` | Not in upstream | Present | Useful for lifecycle — keep |

### RunnerContext

| Feature | Upstream (217 lines) | Shim (30 lines) | Delta |
|---------|---------------------|-----------------|-------|
| `AutoPickTriggerStopCount` | Present + StopAutoPick/ResumeAutoPick | Only the field | **Incomplete** — lacks coordination logic |
| All other fields | 13 fields, 6 methods | None | Not needed for current Core scope |

---

## 6. Dependency Classification

### Pure C# — Can Port As-Is
- `IsInitialized`, `LinkedStartGenshinTime`, `GetGenshinGameProcessNameList()` (TaskContext)
- `IsContinuousRunGroup`, `IsSuspend`, `IsPreExecution`, `isAutoFetchDispatch`, `PartyName`, `AutoPickTriggerStopCount`, `StopAutoPick()`, `ResumeAutoPick()`, `StopAutoPickRunTask()` (RunnerContext)
- `Config.AutoPickConfig` accessor

### Windows-Only — Guarded or Abstracted
- `Init(IntPtr hWnd)` → abstract to `Init(IGameWindowInfo)`
- `GameHandle` → via `IPlatformWindowService`
- `PostMessageSimulator` → `BackgroundClick()` already #if guarded; no macOS equivalent
- `DpiScale` → `ISystemInfo.ScaleTo1080PRatio` already provides this

### Not in Core Scope — Defer
- `CurrentScriptProject` (ClearScript)
- `CombatScenes` (AutoFight.Model)
- `TaskProgress` (TaskProgress)
- `ISuspendable` (AutoPathing)
- `HardwareAccelerationConfig` (GPU acceleration)
- `KeyBindingsConfig` (key binding UI)

---

## 7. Recommended Migration Order

### Phase A: Interface Extraction (no Shim deletion)
1. Define `ITaskContextCore` with: `SystemInfo`, `Config` accessor for AutoPick scope, `IsInitialized`
2. Define `IAutoPickRuntimeState` with: `AutoPickTriggerStopCount`, `StopAutoPick()`, `ResumeAutoPick()`
3. Keep existing Shims as temporary adapters

### Phase B: Upstream Refactoring
4. Make upstream `TaskContext` implement `ITaskContextCore`
5. Extract `AutoPickTriggerStopCount` + coordination methods into a separate class or interface
6. Windows-only fields (`GameHandle`, `PostMessageSimulator`) remain on the Windows TaskContext only — not on the interface

### Phase C: Core Migration
7. Replace Shim `TaskContext` / `CoreConfig` with real upstream TaskContext adapted for Core
8. Replace Shim `RunnerContext` with `IAutoPickRuntimeState` implementation
9. At this point: delete the Shims, link the real upstream files

### What NOT to Do
- ❌ Do not add `#if BGI_FULL_WINDOWS` to upstream TaskContext — it would fragment the file
- ❌ Do not expand Shim CoreConfig with more hand-written config stubs
- ❌ Do not delete upstream members to make it "compile" on macOS
- ❌ Do not create a parallel TaskContext with the same name in another namespace

---

## 8. Current Shim Retention Justification

| Shim File | Reason to Keep (short-term) | Long-term Disposition |
|-----------|-----------------------------|----------------------|
| `TaskContext.cs` | Provides AutoPick with `SystemInfo` + `Config` access | Delete after Phase C |
| `RunnerContext.cs` | Provides `AutoPickTriggerStopCount` | Delete after Phase B |
| `CoreConfig` | Avoids linking full `AllConfig` with 20+ task config types | Delete after Config accessor refactoring |

---

## 9. Related Audit: Region Chain (Completed)

Region/DesktopRegion/GameCaptureRegion: ✅ Real upstream files now linked.
Drawing/mouse methods guarded with minimal `#if BGI_FULL_WINDOWS`.
Core recognition path (Find, Derive, ConvertRes, coordinate transforms) is 100% authentic upstream code.
