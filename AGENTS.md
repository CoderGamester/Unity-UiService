# GameLovers.UiService - AI Agent Guide

> **Companion files**: `CLAUDE.md` wraps this file for Claude Code — edit `AGENTS.md`, not `CLAUDE.md`. `docs/README.md` (and linked pages) is the user-facing documentation set.

## 1. Package Overview
- **Package**: `com.gamelovers.uiservice`
- **Unity**: 6000.0+
- **Dependencies** (see `package.json`)
  - `com.unity.addressables` (2.6.0)
  - `com.cysharp.unitask` (2.5.10)
  - `com.unity.render-pipelines.universal` (17.0.1) — required by the `Runtime/Rendering/` presenter features (camera stacking, render-texture targets, backdrop blur); floor version matches this package's `unity: 6000.0` (declaring the 17.5.0 bundled with newer editors would silently raise the effective Unity requirement — UPM resolves upward regardless of the floor)

This package provides a centralized UI management service that coordinates presenter **load/open/close/unload**, supports **layering**, **UI sets**, and **multi-instance** presenters, and integrates with **Addressables** + **UniTask**.

For user-facing docs, treat `docs/README.md` (and linked pages) as the primary documentation set. This file is for contributors/agents working on the package itself.

## 2. Runtime Architecture (high level)
- **Service core**: `Runtime/UiService.cs` (`UiService : IUiServiceInit`)
  - Owns configs, loaded presenter instances, visible list, and UI set configs.
  - Creates a `DontDestroyOnLoad` parent GameObject named `"Ui"` and attaches `UiServiceMonoComponent` for resolution/orientation tracking.
  - Tracks presenters as **instances**: `Dictionary<Type, IList<UiInstance>>` where each `UiInstance` stores `(Type, Address, UiPresenter)`.
  - **Editor support**: `UiService.CurrentService` is an **internal** static reference used by editor windows to access the active service in play mode.
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
  - `UiToolkitPresenterFeature.PanelSettings` is read-only by contract — see §4 for why there is no per-presenter `PanelSettings` feature.
- **URP presenter features**: `Runtime/Rendering/*` (namespace `GameLovers.UiService.Rendering`) compile into the main `GameLovers.UiService` assembly, which references `Unity.RenderPipelines.Universal.Runtime` + `Unity.RenderPipelines.Core.Runtime` directly. No quarantine assembly — the URP dependency in `package.json` is hard, so URP is always present.
  - `UiCameraStackFeature`: Screen Space - Camera + URP `cameraStack` insertion. **Two non-obvious requirements, both of which shipped broken and were caught only by running the sample:** (1) the overlay camera must be `enabled` while stacked -- `UniversalRenderPipeline.RenderCameraStack` does `if (!overlayCamera.isActiveAndEnabled) continue`, so a disabled camera sits in the stack and renders nothing; (2) the overlay camera must **not** be a child of the canvas it renders -- a Screen Space - Camera canvas drives its own transform from its `worldCamera`, so a child camera is dragged onto the canvas plane and sees nothing. `DetachCameraFromCanvas` re-parents it out, after which the feature owns its destruction. Both requirements are now pinned by tests in `UiCameraStackFeatureTests`: (1) by the enabled/`isActiveAndEnabled` assertions in the stacking tests (`StackedCamera_IsEnabled_SoUrpActuallyRendersIt`, `UnstackedCamera_IsDisabledAgain_OnClose`); (2) by `Open_WhenOverlayCameraIsChildOfItsOwnCanvas_DetachesCameraFromCanvas`, which asserts `DetachCameraFromCanvas` re-parents the camera out of its own canvas and resets its local position. Base camera via `Func<Camera> BaseCameraResolver` (default `Camera.main`, ~16ns/call and resolved twice per open — do not add caching). Ordering maths in `UiCameraStackFeature.InsertIndex`; per-camera priority in a private static `Dictionary<Camera, int>`. Do not derive priority from the hierarchy — the serialized camera need not be a child, so `GetComponentInParent` silently yields 0 (caught by `MultipleStackedCameras_OrderByCanvasSortingOrder`).
  - **No render-texture feature, deliberately.** It is Inspector-authorable in both UI systems (`Camera.m_TargetTexture` + `Canvas.m_RenderMode`; `PanelSettings.m_TargetTexture`). A `UiRenderTextureFeature` was removed as ~180 lines wrapping three Inspector fields — document the authoring recipe instead of re-adding it.
  - `UiBackdropBlurRendererFeature` (`ScriptableRendererFeature`, added to the project's URP Renderer asset) authors the blur's look — downsample, iterations, spread, tint — because that is one project-wide art decision, not per-presenter state. `UiBackdropBlurPresenterFeature` only holds a static refcount open while the presenter is visible, exposed as `AnyOpen`. A `UiBackdropBlurRequests` registry keyed by presenter with `sortKey` resolution was removed: it existed solely to answer "whose settings win", a question that disappears once the look lives on the asset. The camera gating is `UiBackdropBlurPass.ShouldInject`.
  - **Test asmdefs reference URP directly.** Calling a static on a type whose *base class* lives in the URP assembly (`UiBackdropBlurPass : ScriptableRenderPass`) needs that reference — `CS0012` otherwise. A base type in this assembly (`UiCameraStackFeature : PresenterFeatureBase`) does not. Do not move code around to keep a test asmdef URP-free; this package is URP-only, so the reference is honest.
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
    - `Rendering/*` — **URP presenter features** (camera stacking, render-texture targets, backdrop blur). Part of the main `GameLovers.UiService` assembly; the folder is an organizational boundary, not an assembly one. See §2 for the per-type breakdown.
- **Editor**: `Editor/` (assembly: `Editor/GameLovers.UiService.Editor.asmdef`)
  - Config editors: `UiConfigsEditorBase.cs`, `*UiConfigsEditor.cs`, `DefaultUiConfigsEditor.cs`.
  - Debugging: `UiPresenterManagerWindow.cs`, `UiPresenterEditor.cs`.
- **Samples**: `Samples~/`
  - Demonstrates basic flows, data presenters, delay features, UI Toolkit integration.
  - `Samples~/UrpRendering/` is the only sample that requires URP. It ships sample-scoped editor automation (`Editor/UrpRenderingSampleSetup.cs` + its own asmdef) that installs `UiBackdropBlurRendererFeature` onto every Renderer asset the active URP pipeline uses, because the blur is otherwise a silent no-op. It replicates URP's internal `ScriptableRendererDataEditor.AddComponent`: the feature must be added as a sub-asset **and** registered in `m_RendererFeatureMap` by local file id, or the list deserializes with a null entry. Idempotent, with an `[InitializeOnLoadMethod]` safety net for the UPM first-import ordering problem.
- **Tests**: `Tests/`
  - Before reading, editing, or creating any file in `Tests/`, you **MUST** read [`Tests/AGENTS.md`](Tests/AGENTS.md) first.
  - Test asmdef layout, MonoBehaviour-presenter placement, the `Measure.Method` performance pattern, `LogAssert.Expect` scope, and TMPro/`UnityEngine.UI` test-asmdef references now live there — do not duplicate them here.

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
- **`UiConfig.Layer` is not a total order once render modes mix**
  - Screen Space - Overlay canvases composite after ALL URP rendering, straight to the backbuffer. A `UiCameraStackFeature` presenter (Screen Space - Camera, URP-stacked) at a high `Layer` still draws underneath an Overlay presenter at `Layer` 0 the moment both exist in the same scene. This is a fundamental property of how the two render modes composite, not a bug — there is no runtime fix. Keep Overlay and camera-stacked presenters visually separated (e.g. HUD as Overlay, world-anchored UI as stacked), or accept the fixed relative order.
  - `UiCameraStackFeature` is incompatible with pointing that presenter's camera at a `RenderTexture`: URP requires a stacked overlay camera to share the base camera's render target, but a render-texture-target camera by definition renders to its own texture. Pick one per presenter.
- **Never mutate `PanelSettings` at runtime, and don't add a feature that does**
  - N `UIDocument`s sharing one `PanelSettings` share **one panel**, ordered via `UIDocument.sortingOrder` (what `UiService` sets from `UiConfig.Layer`). Give a presenter its own `PanelSettings` instance and it gets its own panel, at which point cross-presenter ordering for that presenter switches to `PanelSettings.sortingOrder` and `UiConfig.Layer` silently stops applying to it.
  - A `UiPanelSettingsFeature` that cloned `PanelSettings` per presenter was removed: it served only a UI Toolkit render-texture path that `PanelRenderMode.WorldSpace` supersedes, needed a second component on every prefab, and per-presenter panels defeat batching. **Author additional `PanelSettings` assets instead.**
  - Do not add a third sorting-order branch to `UiService.EnsureCanvasSortingOrder`. The `Canvas` / `UIDocument` pair it already handles is complete.
- **UI Sets store types, not addresses**
  - UI sets are serialized as `UiSetEntry` (type name + instance address). The default editor populates `InstanceAddress` with the **addressable address** for uniqueness.
- **`LoadSynchronously` persistence**
  - `UiConfig.LoadSynchronously` is serialized through `UiConfigs.UiConfigSerializable` and round-trips correctly. It is respected by `AddressablesUiAssetLoader` for synchronous instantiation.
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
- **UI Toolkit test `PanelSettings` creation** — see [`Tests/AGENTS.md`](Tests/AGENTS.md) §3 for the theme-before-document assignment order that avoids the duplicate "No Theme Style Sheet" warning.

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
  - Add/update the entry in `UiConfigs` (menu: `Tools/GameLovers/UI Configs/Select UI Configs`).
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
- Core runtime systems/features are introduced/removed (features, views, multi-instance)
- Editor tooling changes how configs or sets are generated/serialized