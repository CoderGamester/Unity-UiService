# GameLovers.UiService - AI Agent Guide

## 1. Package Overview
- **Package**: `com.gamelovers.uiservice`
- **Unity**: 6000.0+
- **Dependencies** (see `package.json`)
  - `com.unity.addressables` (2.6.0)
  - `com.cysharp.unitask` (2.5.10)

This package provides a centralized UI management service that coordinates presenter **load/open/close/unload**, supports **layering**, **UI sets**, and **multi-instance** presenters, and integrates with **Addressables** + **UniTask**.

For user-facing docs, treat `docs/README.md` (and linked pages) as the primary documentation set. This file is for contributors/agents working on the package itself.

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
  - Stores UI configs as `UiConfigs.UiConfigSerializable` (address + layer + type name) and UI sets as `UiSetConfigSerializable` containing `UiSetEntry` items.
  - Use the specialized subclasses (each has `CreateAssetMenu`): `AddressablesUiConfigs` (default), `ResourcesUiConfigs`, `PrefabRegistryUiConfigs` (embedded prefab registry).
  - Note: `UiConfigs` is `abstract` to prevent accidental direct usage—always use one of the specialized subclasses.
- **UI Sets**: `Runtime/UiSetConfig.cs`
  - `UiSetEntry` stores:
    - presenter type as `AssemblyQualifiedName` string
    - optional `InstanceAddress` (empty string means default instance)
  - `UiSetConfig` is the runtime shape: `SetId` + `UiInstanceId[]`.
- **Presenter pattern**: `Runtime/UiPresenter.cs`
  - Lifecycle hooks: `OnInitialized`, `OnOpened`, `OnClosed`, `OnOpenTransitionCompleted`, `OnCloseTransitionCompleted`.
  - Typed presenters: `UiPresenter<T>` with a `Data` property that triggers `OnSetData()` on assignment (works during `OpenUiAsync(..., initialData, ...)` or later updates).
  - Presenter features are discovered via `GetComponents(_features)` at init time and are notified in the open/close lifecycle.
  - **Transition tasks**: `OpenTransitionTask` and `CloseTransitionTask` are public `UniTask` properties that complete when all transition features finish.
  - **Visibility control**: `UiPresenter` is the single point of responsibility for `SetActive(false)` on close; it waits for all `ITransitionFeature` tasks before hiding.
- **Composable features**: `Runtime/Features/*`
  - `PresenterFeatureBase` allows attaching components to a presenter prefab to hook lifecycle.
  - `ITransitionFeature` interface for features that provide open/close transition delays (presenter awaits these).
  - Built-in transition features: `TimeDelayFeature`, `AnimationDelayFeature`.
  - UI Toolkit support: `UiToolkitPresenterFeature` (via `UIDocument`) provides `AddVisualTreeAttachedListener(callback)` for safe element queries. Callback is invoked on each open because UI Toolkit recreates elements when the presenter is deactivated/reactivated.
- **Helper views**: `Runtime/Views/*` (`GameLovers.UiService.Views`)
  - `SafeAreaHelperView`: adjusts anchors/size based on safe area (notches).
  - `NonDrawingView`: raycast target without rendering (extends `Graphic`).
  - `AdjustScreenSizeFitterView`: layout fitter that clamps between min/flexible size.
  - `InteractableTextView`: TMP link click handling.
- **Asset loading**: `Runtime/Loaders/IUiAssetLoader.cs`
  - `IUiAssetLoader` abstraction with multiple implementations under `Runtime/Loaders/`:
    - `AddressablesUiAssetLoader` (default): uses `Addressables.InstantiateAsync` and `Addressables.ReleaseInstance`.
    - `PrefabRegistryUiAssetLoader`: uses direct prefab references (useful for samples/testing). Can be initialized with a `PrefabRegistryUiConfigs` in its constructor.
    - `ResourcesUiAssetLoader`: uses `Resources.Load`.
  - Supports optional synchronous instantiation via `UiConfig.LoadSynchronously` (in Addressables loader).
- **Analytics (optional)**: `Runtime/UiAnalytics.cs`
  - `IUiAnalytics`/`UiAnalytics` track lifecycle events + basic timings; defaults to `NullAnalytics`.
  - `UiService.CurrentAnalytics` is an **internal** static reference used by editor windows.

## 3. Key Directories / Files
- **Docs (user-facing)**: `docs/`
  - `docs/README.md` — documentation entry point.
- **Runtime**: `Runtime/`
  - Entry points: `IUiService.cs`, `UiService.cs`, `UiPresenter.cs`, `UiConfigs.cs`, `UiSetConfig.cs`, `UiInstanceId.cs`.
  - Integrations / extension points (start here when behavior differs from expectations):
    - `Loaders/*` — **how presenter prefabs are instantiated / released**.
      - If UI fails to load/unload, start at `Loaders/IUiAssetLoader.cs` and the active loader (`AddressablesUiAssetLoader`, `ResourcesUiAssetLoader`, `PrefabRegistryUiAssetLoader`).
      - Loader choice is typically driven by which `UiConfigs` subclass you use (`AddressablesUiConfigs` / `ResourcesUiConfigs` / `PrefabRegistryUiConfigs`).
    - `Features/*` — **presenter composition** (components attached to presenter prefabs).
      - Lifecycle hooks live in `PresenterFeatureBase`; features are discovered during presenter initialization.
      - Transition timing issues (UI not showing/hiding when expected) usually involve `ITransitionFeature` implementations (eg `TimeDelayFeature`, `AnimationDelayFeature`).
      - UI Toolkit presenters rely on `UiToolkitPresenterFeature`; avoid querying `UIDocument.rootVisualElement` during `OnInitialized()`—use `AddVisualTreeAttachedListener(...)`.
    - `Views/*` — **optional helper components** used by presenter prefabs (safe area, raycasts, layout fitters, TMP link clicks).
      - If interaction/layout is off but service bookkeeping looks correct, look here before changing `UiService`.
- **Editor**: `Editor/` (assembly: `Editor/GameLovers.UiService.Editor.asmdef`)
  - Config editors: `UiConfigsEditorBase.cs`, `*UiConfigsEditor.cs`, `DefaultUiConfigsEditor.cs`.
  - Debugging: `UiAnalyticsWindow.cs`, `UiServiceHierarchyWindow.cs`, `UiPresenterEditor.cs`.
- **Samples**: `Samples~/`
  - Demonstrates basic flows, data presenters, delay features, UI Toolkit integration, analytics.
- **Tests**: `Tests/`
  - `Tests/EditMode/*` — unit tests (configs, sets, analytics, loaders, core service behavior)
  - `Tests/PlayMode/*` — integration/performance/smoke tests

## 4. Important Behaviors / Gotchas
- **Instance address normalization**
  - `UiInstanceId` normalizes `null/""` to `string.Empty`.
  - Prefer **`string.Empty`** as the default/singleton instance identifier.
- **Ambiguous “default instance” calls**
  - `UiService` uses an internal `ResolveInstanceAddress(type)` when an API is called without an explicit `instanceAddress`.
  - If **multiple instances** exist, it logs a warning and selects the **first** instance. For multi-instance usage, prefer calling `UiService` overloads that include `instanceAddress`.
- **Presenter self-close + destroy with multi-instance**
  - `UiPresenter.Close(destroy: true)` now correctly uses the presenter's stored `InstanceAddress` to unload the correct instance.
  - This works seamlessly for both singleton and multi-instance presenters.
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
- **UI Toolkit visual tree timing and element recreation**
  - `UIDocument.rootVisualElement` may not be ready when `OnInitialized()` is called on a presenter.
  - UI Toolkit **recreates visual elements** when the presenter GameObject is deactivated/reactivated (close/reopen cycle), `AddVisualTreeAttachedListener(callback)` invokes  on **each open** to handle element recreation.

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
  - Use `UiInstanceId` (default = `string.Empty`) when you need to track instances externally.
  - Presenters know their own instance address via the internal `InstanceAddress` property; `Close(destroy: true)` unloads the correct instance.
- **Add a presenter feature**
  - Extend `PresenterFeatureBase` and attach it to the presenter prefab.
  - Features are discovered via `GetComponents` at init time and notified during open/close.
  - For features with transitions (animations, delays): implement `ITransitionFeature` so the presenter can await your `OpenTransitionTask` / `CloseTransitionTask`.
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