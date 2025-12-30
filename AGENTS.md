# GameLovers.UiService - AI Agent Guide

## 1. Package Overview
- **Package**: `com.gamelovers.uiservice`
- **Unity**: 6000.0+
- **Dependencies** (see `package.json`)
  - `com.unity.addressables` (2.6.0)
  - `com.cysharp.unitask` (2.5.10)

This package provides a centralized UI management service that coordinates presenter **load/open/close/unload**, supports **layering**, **UI sets**, and **multi-instance** presenters, and integrates with **Addressables** + **UniTask**.

For user-facing docs, treat `docs/README.md` (and linked pages) as the primary documentation set; this file is for contributors/agents working on the package itself.

## 2. Runtime Architecture (high level)
- **Service core**: `Runtime/UiService.cs` (`UiService : IUiServiceInit`)
  - Owns configs, loaded presenter instances, visible list, and UI set configs.
  - Creates a `DontDestroyOnLoad` parent GameObject named `"Ui"` and attaches `UiServiceMonoComponent` for resolution/orientation tracking.
  - Tracks presenters as **instances**: `Dictionary<Type, IList<UiInstance>>` where each `UiInstance` stores `(Type, Address, UiPresenter)`.
- **Public API surface**: `Runtime/IUiService.cs`
  - Exposes lifecycle operations (load/open/close/unload) and readonly views:
    - `VisiblePresenters : IReadOnlyList<UiInstanceId>`
    - `UiSets : IReadOnlyDictionary<int, UiSetConfig>`
    - `GetLoadedPresenters() : List<UiInstance>`
  - Note: **multi-instance overloads** (explicit `instanceAddress`) exist on `UiService` (concrete type), not on `IUiService`.
- **Configuration**: `Runtime/UiConfigs.cs` (`ScriptableObject`)
  - Stores UI configs as `UiConfigs.UiConfigSerializable` (address + layer + type name).
  - Stores UI sets as `UiSetConfigSerializable` containing `UiSetEntry` items.
- **UI Sets**: `Runtime/UiSetConfig.cs`
  - `UiSetEntry` stores:
    - presenter type as `AssemblyQualifiedName` string
    - optional `InstanceAddress` (empty string means default instance)
  - `UiSetConfig` is the runtime shape: `SetId` + `UiInstanceId[]`.
- **Presenter pattern**: `Runtime/UiPresenter.cs`
  - Lifecycle hooks: `OnInitialized`, `OnOpened`, `OnClosed`.
  - Typed presenters: `UiPresenter<T>` (data assigned during open via `OpenUiAsync(type, initialData, ...)`).
  - Presenter features are discovered via `GetComponents(_features)` at init time and are notified in the open/close lifecycle.
- **Composable features**: `Runtime/Features/*`
  - `PresenterFeatureBase` allows attaching components to a presenter prefab to hook lifecycle.
  - Built-in features:
    - `TimeDelayFeature`
    - `AnimationDelayFeature`
    - `UiToolkitPresenterFeature` (UI Toolkit support via `UIDocument`)
- **Helper views**: `Runtime/Views/*` (`GameLovers.UiService.Views`)
  - `SafeAreaHelperView`: adjusts anchors/size based on safe area (notches).
  - `NonDrawingView`: raycast target without rendering (extends `Graphic`).
  - `AdjustScreenSizeFitterView`: layout fitter that clamps between min/flexible size.
  - `InteractableTextView`: TMP link click handling.
- **Asset loading**: `Runtime/Loaders/IUiAssetLoader.cs`
  - `IUiAssetLoader` abstraction with multiple implementations under `Runtime/Loaders/`:
    - `AddressablesUiAssetLoader` (default): uses `Addressables.InstantiateAsync` and `Addressables.ReleaseInstance`.
    - `PrefabRegistryUiAssetLoader`: uses direct prefab references (useful for samples/testing).
    - `ResourcesUiAssetLoader`: uses `Resources.Load`.
  - Supports optional synchronous instantiation via `UiConfig.LoadSynchronously` (in Addressables loader).
- **Analytics (optional)**: `Runtime/UiAnalytics.cs`
  - `IUiAnalytics` + `UiAnalytics` track lifecycle events and simple performance timings.
  - `UiService` defaults to `NullAnalytics` when none is provided.
  - `UiService.CurrentAnalytics` is an **internal** static reference used by editor windows in this package.

## 3. Key Directories / Files
- **Docs (user-facing)**: `docs/`
  - `docs/README.md` — doc entry point (Getting Started, Core Concepts, API Reference, Advanced, Troubleshooting)
- **Runtime**: `Runtime/`
  - `Runtime/IUiService.cs` — public API surface + `IUiServiceInit`
  - `Runtime/UiService.cs` — core implementation (multi-instance, sets, analytics hooks)
  - `Runtime/UiPresenter.cs` — presenter base classes + feature hooks
  - `Runtime/UiConfigs.cs` — config storage/serialization
  - `Runtime/UiSetConfig.cs` — UI set + serialization helpers
  - `Runtime/UiInstanceId.cs` — multi-instance identity (normalizes null/empty to `string.Empty`)
  - `Runtime/Loaders/IUiAssetLoader.cs` — Asset loading abstraction
  - `Runtime/Loaders/AddressablesUiAssetLoader.cs` — Addressables integration (+ optional sync load)
  - `Runtime/Loaders/PrefabRegistryUiAssetLoader.cs` — Direct prefab references
  - `Runtime/Loaders/ResourcesUiAssetLoader.cs` — Resources.Load implementation
  - `Runtime/UiAnalytics.cs` — analytics + metrics
  - `Runtime/UiServiceMonoComponent.cs` — emits resolution/orientation change events
  - `Runtime/Features/*` — composable presenter features
  - `Runtime/Views/*` — helper view components (safe area, non-drawing, sizing, TMP links)
- **Editor**: `Editor/` (assembly: `Editor/GameLovers.UiService.Editor.asmdef`)
  - `Editor/UiConfigsEditor.cs` — UI Toolkit inspector for `UiConfigs` + layer visualizer + menu items
  - `Editor/DefaultUiConfigsEditor.cs` — default out-of-the-box `UiConfigs` editor using `DefaultUiSetId`
  - `Editor/UiAnalyticsWindow.cs` — play-mode analytics viewer
  - `Editor/UiServiceHierarchyWindow.cs` — play-mode hierarchy/debug window
  - `Editor/UiPresenterEditor.cs` — quick open/close controls for a `UiPresenter` in play mode
  - `Editor/NonDrawingViewEditor.cs` — inspector for `NonDrawingView` (raycast controls)
- **Samples**: `Samples~/`
  - Demonstrates basic flows, data presenters, delay features, UI Toolkit integration, analytics.
- **Tests**: `Tests/`
  - `Tests/EditMode/*` — unit tests (configs, sets, analytics, loaders, core service behavior)
  - `Tests/EditMode/Loaders/*` — unit tests for asset loader implementations
  - `Tests/PlayMode/*` — integration/performance/smoke tests + fixtures/prefabs

## 4. Important Behaviors / Gotchas
- **Instance address normalization**
  - `UiInstanceId` normalizes `null/""` to `string.Empty`.
  - Prefer **`string.Empty`** as the default/singleton instance identifier.
- **Ambiguous “default instance” calls**
  - `UiService` uses an internal `ResolveInstanceAddress(type)` when an API is called without an explicit `instanceAddress`.
  - If **multiple instances** exist, it logs a warning and selects the **first** instance. For multi-instance usage, prefer calling `UiService` overloads that include `instanceAddress`.
- **Presenter self-close + destroy with multi-instance**
  - `UiPresenter.Close(destroy: true)` ultimately calls `_uiService.UnloadUi(GetType())` (no instance address), which can be ambiguous when multiple instances exist.
  - For multi-instance cleanup, close/unload via `UiService` overloads using `(Type, instanceAddress)` (or by tracking your own `UiInstanceId` externally).
- **Layering**
  - `UiService` enforces sorting by setting `Canvas.sortingOrder` or `UIDocument.sortingOrder` to the config layer when adding/loading.
  - Loaded presenters are instantiated under the `"Ui"` root directly (no per-layer container GameObjects).
- **UI Sets store types, not addresses**
  - UI sets are serialized as `UiSetEntry` (type name + instance address). The default editor populates `InstanceAddress` with the **addressable address** for uniqueness.
- **`LoadSynchronously` persistence**
  - `UiConfig.LoadSynchronously` exists and is respected by `AddressablesUiAssetLoader`.
  - **However**: `UiConfigs.UiConfigSerializable` currently does **not** serialize `LoadSynchronously`, so configs loaded from a `UiConfigs` asset will produce `LoadSynchronously = false` in `UiConfigs.Configs`.
- **Static events**
  - `UiService.OnResolutionChanged` / `UiService.OnOrientationChanged` are static `UnityEvent`s raised by `UiServiceMonoComponent`.
  - The service does not clear listeners; consumers must unsubscribe appropriately.
- **Disposal**
  - `UiService.Dispose()` closes all visible UI, attempts to unload all loaded instances, clears collections, and destroys the `"Ui"` root GameObject.
- **Editor debugging tools**
  - Some editor windows toggle `presenter.gameObject.SetActive(...)` directly for convenience; this may not reflect in `IUiService.VisiblePresenters` since it bypasses `UiService` bookkeeping.

## 5. Coding Standards (Unity 6 / C# 9.0)
- **C#**: C# 9.0 syntax; no global `using`s; keep **explicit namespaces**.
- **Assemblies**
  - Runtime code should avoid `UnityEditor` references; editor-only tooling belongs under `Editor/` and `GameLovers.UiService.Editor.asmdef`.
  - If you must add editor-only code near runtime types, guard it with `#if UNITY_EDITOR` and keep it minimal.
- **Async**
  - Use `UniTask`; thread `CancellationToken` through async APIs where available.
- **Memory / allocations**
  - Avoid per-frame allocations; keep API properties allocation-free (see `UiService` read-only wrappers for `VisiblePresenters` and `UiSets`).

## 6. External Package Sources (for API lookups)
When you need third-party source/docs, prefer the locally-cached UPM packages:
- Addressables: `Library/PackageCache/com.unity.addressables@*/`
- UniTask: `Library/PackageCache/com.cysharp.unitask@*/`

## 7. Dev Workflows (common changes)
- **Add a new presenter**
  - Create a prefab with a component deriving `UiPresenter` (or `UiPresenter<T>`).
  - Ensure it has a `Canvas` or `UIDocument` if you want layer sorting to apply.
  - Mark the prefab Addressable and set its address.
  - Add/update the entry in `UiConfigs` (menu: `Tools/UI Service/Select UiConfigs`).
- **Add / update UI sets**
  - The default `UiConfigs` inspector uses `DefaultUiSetId` (out-of-the-box).
  - To customize set ids, create your own enum and your own `[CustomEditor(typeof(UiConfigs))] : UiConfigsEditor<TEnum>`.
- **Add multi-instance flows**
  - Use `UiInstanceId` (default = `string.Empty`) and prefer calling `UiService` overloads that take `instanceAddress` when multiple instances may exist.
- **Add a presenter feature**
  - Extend `PresenterFeatureBase` and attach it to the presenter prefab.
  - Features are discovered via `GetComponents` at init time and notified during open/close.
- **Change loading strategy**
  - Prefer using one of the built-in loaders (`AddressablesUiAssetLoader`, `PrefabRegistryUiAssetLoader`, `ResourcesUiAssetLoader`) or extending `IUiAssetLoader` for custom needs.
- **Update docs/samples**
  - User-facing docs live in `docs/` and should be updated when behavior/API changes.
  - If you add a new core capability, consider adding/adjusting a sample under `Samples~/`.

## 8. Update Policy
Update this file when:
- Public API changes (`IUiService`, `IUiServiceInit`, presenter lifecycle, config formats)
- Core runtime systems/features are introduced/removed (features, views, analytics, multi-instance)
- Editor tooling changes how configs or sets are generated/serialized