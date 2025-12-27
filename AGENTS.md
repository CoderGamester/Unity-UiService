# GameLovers.UiService - AI Agent Guide

## 1. Package Overview
**Package**: `com.gamelovers.uiservice`  
**Unity**: 6000.0+  
**Dependencies**: Addressables, UniTask (see `package.json`)

This package provides a UI management service that coordinates UI presenter **load/open/close/unload**, supports **layering**, **UI sets**, and **multi-instance** presenters, and integrates with **Addressables** + **UniTask**.

## 2. Runtime Architecture (high level)
- **Service core**: `Runtime/UiService.cs` (`IUiServiceInit`)
  - Owns UI configs, loaded presenter instances, visible list, and set configs.
  - Creates a `DontDestroyOnLoad` parent GameObject named `"Ui"` and attaches `UiServiceMonoComponent` for screen/orientation tracking.
- **Configuration**: `Runtime/UiConfigs.cs` (`ScriptableObject`)
  - Holds `UiConfig` entries (type + addressable address + layer + load-sync flag).
  - Holds `UiSetConfig` entries (grouped UIs by `setId`).
- **Presenter pattern**: `Runtime/UiPresenter.cs`
  - Base presenter lifecycle hooks: `OnInitialized`, `OnOpened`, `OnClosed`.
  - Optional typed-data presenters: `UiPresenter<T>` (data set on open).
- **Composable features**: `Runtime/Features/*`
  - Implement `IPresenterFeature` and attach to presenter GameObjects to extend lifecycle (delays, UI Toolkit integration, etc.).
- **Asset loading**: `Runtime/UiAssetLoader.cs`
  - `IUiAssetLoader` abstraction; default implementation instantiates via `Addressables.InstantiateAsync` and unloads via `Addressables.ReleaseInstance`.
- **Analytics (optional)**: `Runtime/UiAnalytics.cs`
  - `IUiAnalytics` is injectable; `UiService` falls back to `NullAnalytics`.
  - `UiService.CurrentAnalytics` is an **internal** static reference intended for editor tooling/inspection.

## 3. Key Directories / Files
- **Runtime**
  - `Runtime/IUiService.cs` — public API surface
  - `Runtime/UiService.cs` — core implementation
  - `Runtime/UiPresenter.cs` — presenter base classes
  - `Runtime/UiAssetLoader.cs` — Addressables integration
  - `Runtime/UiConfigs.cs` — configs + sets storage/serialization helpers
  - `Runtime/UiInstanceId.cs` — multi-instance identity
  - `Runtime/UiServiceMonoComponent.cs` — emits resolution/orientation change events
  - `Runtime/UiAnalytics.cs` — analytics + performance metrics
- **Editor**
  - `Editor/UiConfigsEditor.cs` — config authoring; sets `UiConfig.LoadSynchronously` using `LoadSynchronouslyAttribute`
  - `Editor/UiPresenterEditor.cs` — quick controls for presenters
  - `Editor/UiAnalyticsWindow.cs`, `Editor/UiServiceHierarchyWindow.cs` — debugging/monitoring
- **Tests**
  - `Tests/EditMode/Helpers/MockAssetLoader.cs` — avoids Addressables
  - `Tests/EditMode/Helpers/TestHelpers.cs` — presenter/prefab helpers
  - `TESTING_PLAN.md` — source-of-truth test strategy and coverage goals

## 4. Important Behaviors / Gotchas
- **Default instance address**:
  - `UiInstanceId` normalizes `null/""` to `string.Empty`.
  - Prefer **`string.Empty`** for the default/singleton instance and avoid passing `null` around unless you’re sure the call site normalizes it.
- **Layering**:
  - `UiService` enforces sorting by setting `Canvas.sortingOrder` or `UIDocument.sortingOrder` to the config layer when adding/loading.
- **Close vs Unload**:
  - `CloseUi(..., destroy: false)` hides the presenter (`SetActive(false)`).
  - `CloseUi(..., destroy: true)` ultimately unloads via `UnloadUi` (Addressables release + removal from service).
- **Static events**:
  - `UiService.OnResolutionChanged` / `UiService.OnOrientationChanged` are static `UnityEvent`s.
  - The service does not clear listeners; consumers must unsubscribe appropriately.
- **Disposal**:
  - `UiService.Dispose()` attempts to close/unload everything and destroys the `"Ui"` root GameObject. Call it in tests / controlled lifetimes.

## 5. Coding Standards (Unity 6 / C# 9.0)
- **C#**: C# 9.0 syntax; no global `using`s; keep **explicit namespaces**.
- **Assemblies**:
  - Runtime code must not reference `UnityEditor`.
  - Editor tools live under `Editor/` and `GameLovers.UiService.Editor.asmdef`.
- **Async**:
  - Use `UniTask` for async flows; thread through `CancellationToken` where available.
- **Memory / allocations**:
  - Avoid per-frame allocations; keep API properties allocation-free (see `UiService` read-only wrappers pattern).

## 6. External Package Sources (for API lookups)
When you need third-party source/docs, prefer the locally-cached UPM packages:
- Addressables: `Library/PackageCache/com.unity.addressables@*/`
- UniTask: `Library/PackageCache/com.cysharp.unitask@*/`

## 7. Dev Workflows (common changes)
- **Add a new presenter**
  - Create a prefab with a component deriving `UiPresenter` (or `UiPresenter<T>`).
  - Mark the prefab as Addressable and set its address.
  - Add/update the entry in `UiConfigs` via the custom editor tooling.
- **Add a presenter feature**
  - Implement `IPresenterFeature` (or extend `PresenterFeatureBase`), then attach it to the presenter prefab.
- **Change loading strategy**
  - Prefer extending/replacing `IUiAssetLoader` for tests/special loading; keep Addressables calls centralized in `UiAssetLoader`.

## 8. Update Policy
Update this file when:
- Public API changes (`IUiService`, presenter lifecycle, config formats)
- New core runtime systems/features are introduced
- Editor tooling changes how configs are generated or interpreted


