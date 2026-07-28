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
  - `Runtime/Features/UiPanelSettingsFeature.cs` (core assembly, no URP dependency): clones a presenter's `PanelSettings` per-instance instead of mutating the shared asset every `UIDocument` referencing it would otherwise see. `SetTargetTexture(RenderTexture)` is the seam `UiRenderTextureFeature`'s UI Toolkit path uses — never assign `PanelSettings.targetTexture` directly.
- **URP presenter features**: `Runtime/Rendering/*` (second assembly `GameLovers.UiService.Urp`, namespace `GameLovers.UiService.Rendering`, references `Unity.RenderPipelines.Universal.Runtime` + `Unity.RenderPipelines.Core.Runtime`) — quarantines the URP dependency to one folder so the core assembly stays URP-free; a future retreat from the hard dependency becomes a folder move.
  - `UiCameraStackFeature`: Screen Space - Camera + URP `cameraStack` insertion for a prefab-child overlay camera. Base camera resolved via swappable `IUiBaseCameraProvider` (default `UiBaseCameraProvider`, wraps `Camera.main`). Ordering math lives in the URP-free `UiCameraStackRegistry.InsertIndex`; per-camera priority is tracked via a `UiCameraStackPriorityMarker` component on the overlay camera itself (no separate static registry to leak).
  - `UiRenderTextureFeature`: routes to `UiRenderTextureMode.Canvas` (Screen Space - Camera + dedicated render `Camera`) or `.UiToolkit` (via `UiPanelSettingsFeature.SetTargetTexture`); `Auto` resolves from whichever companion feature/component is present.
  - `UiBackdropBlurFeature` (`ScriptableRendererFeature`, added to the consumer's URP Renderer asset) + `UiBackdropBlurPresenterFeature` (registers/unregisters a blur request while open). Injection is gated by the URP-free `UiBackdropBlurInjection.ShouldInject` predicate and resolved by `UiBackdropBlurRequests` (static, refcounted, highest-`sortKey`-wins).
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
    - `Rendering/*` — **URP presenter features** (camera stacking, render-texture targets, backdrop blur). Own assembly (`GameLovers.UiService.Urp.asmdef`) so the URP dependency stays out of the core assembly. See §2 for the per-type breakdown.
- **Editor**: `Editor/` (assembly: `Editor/GameLovers.UiService.Editor.asmdef`)
  - Config editors: `UiConfigsEditorBase.cs`, `*UiConfigsEditor.cs`, `DefaultUiConfigsEditor.cs`.
  - Debugging: `UiPresenterManagerWindow.cs`, `UiPresenterEditor.cs`.
- **Samples**: `Samples~/`
  - Demonstrates basic flows, data presenters, delay features, UI Toolkit integration.
- **Tests**: `Tests/`
  - `Tests/EditMode/*` — unit tests (configs, sets, loaders, core service behavior). Owned by `GameLovers.UiService.Tests.asmdef` which is **editor-only** (`includePlatforms: ["Editor"]`).
  - `Tests/PlayMode/*` — integration/performance/smoke tests and unit tests that require PlayMode (e.g. `DontDestroyOnLoad`). Owned by `GameLovers.UiService.Tests.PlayMode.asmdef` (runtime-compatible).
  - `Tests/Helpers/*` — **shared test fixtures** consumed by both EditMode and PlayMode. Owned by `GameLovers.UiService.Tests.Helpers.asmdef` (runtime-compatible, gated by `defineConstraints: ["UNITY_INCLUDE_TESTS"]`). **MonoBehaviour-derived test presenters (e.g., `TestUiPresenter`, `TestDataUiPresenter`) MUST live here**, not under `Tests/EditMode/`. Placing a MonoBehaviour in the editor-only EditMode asmdef makes Unity reject `AddComponent<T>()` calls (silent `null` return + warning: `Can't add script behaviour '<name>' because it is an editor script`), which causes tests that create prefabs via `TestHelpers.CreateTestPresenterPrefab<T>` to run without ever attaching the presenter component.
  - **Performance test pattern (`Measure.Method`)**: the body runs `WarmupCount + MeasurementCount` times. Stateful operations against `UiService` (Load/Unload/Open/Close) MUST use `.SetUp()` and/or `.CleanUp()` to reset per-iteration state — otherwise iterations 2+ hit the cache and log `The Ui <X> was already loaded` / `<X> is already open`, and the benchmark measures a no-op cache hit instead of the real operation. Correct shapes:
    - Measuring **Load**: `body = Load; CleanUp = Unload;`
    - Measuring **Unload**: `SetUp = Load; body = Unload;`
    - See `Tests/PlayMode/Performance/PerformanceTests.cs` (`Perf_LoadUi_SinglePresenter`, `Perf_UnloadUi_SinglePresenter`) for the pattern.
  - **`LogAssert.Expect` scope — asserts, does not silence**: `UnityEngine.TestTools.LogAssert.Expect(LogType.Warning, regex)` ensures a matching warning appears during the test (test fails if it doesn't) and prevents the warning from failing the test run for being "unexpected". It does **NOT** suppress the warning from `Editor.log` or the Unity Console — the log line is still emitted. Use it to **pin expected-warning contracts** (service behavior under test), not to reduce console noise. For the latter, restructure the test (see Performance test pattern above) or change the runtime log site — not `LogAssert`.
  - **Test asmdef references for `TMPro` / `UnityEngine.UI` are not transitive**: `GameLovers.UiService.asmdef` (Runtime) references `Unity.TextMeshPro` (`6055be8ebefd69e48b49212b09b47b2f`) and `UnityEngine.UI` is auto-referenced via `noEngineReferences: false`, but `GameLovers.UiService.Tests.PlayMode.asmdef` (test) does **not** inherit those references. When extending tests that touch the `TMPro` / `UnityEngine.UI` namespaces directly (e.g. `InteractableTextView` link-click via `TMP_LinkInfo`, `NonDrawingView` via `VertexHelper`), the test asmdef MUST add `Unity.TextMeshPro` (`6055be8ebefd69e48b49212b09b47b2f`) AND `UnityEngine.UI` (`2bafac87e7f4b9b418d9448d219b01ab`) directly. Symptom of getting this wrong: `CS0246: type or namespace 'TMPro' could not be found` from inside the test asmdef even though the same `using TMPro;` compiles in `Runtime/Views/InteractableTextView.cs`.

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
  - `UiCameraStackFeature` + `UiRenderTextureFeature` are mutually exclusive on the same presenter: URP requires a stacked overlay camera to share the base camera's render target, but a render-texture-target camera by definition renders to its own texture. Do not combine them on one presenter.
- **`UiPanelSettingsFeature` cloning changes what drives layering for UI Toolkit presenters**
  - N `UIDocument`s sharing one `PanelSettings` share one panel, ordered via `UIDocument.sortingOrder` (what `UiService` sets from `UiConfig.Layer`). The moment a presenter clones its `PanelSettings` (any policy other than `UseSharedAsset`), it gets its own panel, and cross-presenter ordering for *that* presenter switches to `PanelSettings.sortingOrder`. `UiPanelSettingsFeature` mirrors `Document.sortingOrder` onto the clone automatically, so this is handled — but do not add a third sorting-order branch to `UiService.EnsureCanvasSortingOrder` to "fix" it further; the mirroring belongs in the feature, not the service.
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
- **UI Toolkit test `PanelSettings` creation — silence the theme warning**
  - A runtime-created `PanelSettings` (no theme asset configured) makes Unity log `No Theme Style Sheet set to PanelSettings , UI will not render properly` **twice** per instantiation: once when assigned to `UIDocument.panelSettings`, and again when the hosting GameObject first goes `SetActive(true)`.
  - Fix in test helpers: assign the theme **before** handing the panel to the document.
    ```csharp
    var panel = ScriptableObject.CreateInstance<PanelSettings>();
    panel.themeStyleSheet = ScriptableObject.CreateInstance<ThemeStyleSheet>(); // empty theme is enough
    document.panelSettings = panel;
    ```
  - See `Tests/PlayMode/Helpers/TestUiToolkitPresenter.cs` and `TestMultiFeatureToolkitPresenter.cs` for the pattern.

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