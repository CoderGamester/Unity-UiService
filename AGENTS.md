# GameLovers UiService — Agent Guide

This guide adds package-specific rules to the host repository guide. Consumer usage belongs in `docs/`.

## Scope

- Package: `com.gamelovers.uiservice`; minimum Unity version and dependencies are authoritative in `package.json`.
- This is the only GameLovers package in this host that intentionally requires URP.
- Runtime owns presenter lifecycle, UI sets, multi-instance identity, loaders, presenter features, helper views, camera stacking, and backdrop blur.
- Public lifecycle contracts begin at `Runtime/IUiService.cs`; the concrete implementation is `Runtime/UiService.cs`.

## Runtime invariants

- Presenter identity is `(type, instanceAddress)`; normalize missing addresses to `string.Empty`. APIs without an explicit address are ambiguous when multiple instances exist and choose the first with a warning.
- Multi-instance overloads with an explicit address live on concrete `UiService`, not `IUiService`. Do not document or consume them as interface members without an intentional API change.
- `UiPresenter` is the single owner of hiding its GameObject after close transitions. Features report transition tasks; they do not independently deactivate the presenter.
- `UiPresenter<T>.Data` invokes `OnSetData()` both for initial open data and later assignments.
- Loader release semantics differ: Addressables releases instances, Resources and prefab registries use their own ownership rules. Preserve loader-specific teardown through `IUiAssetLoader`.
- `UiService.Dispose()` closes/unloads owned presenters, clears state, and destroys the `Ui` root. Static resolution/orientation events are consumer-owned subscriptions and are not globally cleared.
- `UiConfigs` is abstract; use `AddressablesUiConfigs`, `ResourcesUiConfigs`, or `PrefabRegistryUiConfigs`. `UiSetEntry` serializes presenter type plus optional instance address, and synchronous loading is a persisted per-UI config honored only by the Addressables loader.

## URP and UI Toolkit invariants

- `UiCameraStackFeature` overlay cameras must be enabled while stacked and detached from a canvas they render; otherwise URP silently skips them or the canvas moves the camera onto its own plane.
- Camera-stack priority comes from configured canvas sorting, not hierarchy ancestry. The serialized camera need not be a child.
- Resolve the base camera through `BaseCameraResolver` at the points of use. The default `Camera.main` lookup is intentionally not cached because the active main camera may change and the measured lookup cost is negligible.
- Screen Space Overlay composites after URP camera output. `UiConfig.Layer` cannot establish a total order across Overlay and camera-stacked presenters.
- A stacked overlay camera cannot simultaneously target its own `RenderTexture`. There is deliberately no render-texture presenter feature; author `Camera.targetTexture`, canvas render mode, or `PanelSettings.targetTexture` directly.
- Never mutate or clone a shared `PanelSettings` asset at runtime. Shared documents rely on one panel and `UIDocument.sortingOrder`; per-presenter panels change ordering and batching semantics.
- `UiToolkitPresenterFeature` visual trees may not be attached during initialization and are recreated across close/reopen. Use its attachment listener and reacquire elements on each open.
- Backdrop appearance is project-wide renderer-asset configuration. Presenter features contribute only visibility/refcount state; do not add per-presenter mutations to the renderer feature asset.
- Runtime backdrop tuning uses the existing non-serialized static override/sentinel pattern and resets on subsystem registration. Never persist runtime look changes into the shared `ScriptableRendererFeature` asset.
- Test assemblies may reference URP directly when the tested type's base class lives in URP. Do not move code merely to avoid an honest package dependency.

## Optional Rive integration

- `docs/rive-integration.md` owns consumer-facing Rive setup, lifecycle, rendering, input, and
  performance guidance. Update it whenever the public Rive integration contract or samples change.
- The core package and `package.json` never reference Rive. Rive support compiles in the conditional
  `GameLovers.UiService.Rive` assembly only when `app.rive.rive-unity` is installed.
- Every panel in one shared Rive atlas uses `DrawWhenChanged`. One `AlwaysDraw` panel disables native
  dirt checking for the whole atlas, so runtime assignment enforces the invariant and editor tooling
  must report it before Play mode.
- Rive 0.4.3 on Unity 6000.4 or newer requires URP Render Graph. Compatibility Mode bypasses its only
  compiled render-pass path and produces blank textures.
- A Screen Space Camera that samples a Rive render texture before Rive's
  `AfterRenderingTransparents` pass displays the previous frame. Use Screen Space Overlay by default
  and visually validate any camera-space exception.
- `RiveAddressableFileLease` disposes `Rive.File` before releasing its Addressables asset handle.
  Shared render-target strategies are reference-counted and destroyed after their last presenter.

## Assemblies, samples, and tests

- Runtime and URP rendering code compile into `GameLovers.UiService`; Editor tooling stays under `Editor/`.
- Sample-scoped editor setup stays inside the sample. The URP sample must install `UiBackdropBlurRendererFeature` as a renderer-data sub-asset and maintain the renderer feature map idempotently.
- Before changing anything under `Tests/`, read `Tests/AGENTS.md`.
- MonoBehaviour test presenters must live in a runtime-compatible test helper assembly, not the Editor-only EditMode assembly.

## Verification and documentation

- Run both batchmode and Editor PlayMode tests for rendering, Addressables, imported samples, or renderer-feature state.
- Camera stacking, backdrop blur, UI layout, and interaction changes require a fresh inspected Game-view capture in addition to tests.
- Prefer local Addressables, UniTask, and URP sources under `Library/PackageCache/`.
- Update user documentation under `docs/`, samples when appropriate, and `CHANGELOG.md` for notable behavior. Update this guide only for durable package invariants or test conventions.
