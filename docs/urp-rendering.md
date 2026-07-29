# URP Rendering Features

Three presenter features for Universal Render Pipeline projects, living under `Runtime/Rendering/`
(namespace `GameLovers.UiService.Rendering`). This package depends on
`com.unity.render-pipelines.universal` (17.0.1+) — it is a **URP-only package**, and these types compile into
the main `GameLovers.UiService` assembly along with everything else.

Add any of these as components on a presenter prefab alongside its `UiPresenter` subclass, the same way you'd
add `TimeDelayFeature` or `UiToolkitPresenterFeature` — see [Core Concepts](core-concepts.md) for the general
feature-composition pattern.

## UI Toolkit presenters: use `PanelSettings`, don't script it

There is deliberately **no** feature here for per-presenter `PanelSettings`. UI Toolkit already solves this at
authoring time, and better than a runtime component could.

A `PanelSettings` asset *is* a panel. Every `UIDocument` pointing at the same asset shares one panel — which is
exactly what makes cross-presenter ordering via `UIDocument.sortingOrder` (set by `UiService` from
`UiConfig.Layer`) work. So:

- **Never mutate `PanelSettings` at runtime.** It is a shared `ScriptableObject`; writing to it reconfigures
  every other presenter using it, and in the Editor persists that change to disk.
  `UiToolkitPresenterFeature.PanelSettings` is exposed read-only for inspection, with that contract documented
  on the property.
- **If a presenter needs different settings, author a second `PanelSettings` asset** and point that presenter's
  `UIDocument` at it. Group presenters by panel configuration, not one asset per presenter — you want as few
  panels as possible for batching.
- **For world-space UI, set `PanelSettings.renderMode = PanelRenderMode.WorldSpace`** on that asset, then
  position each document via `UIDocument.position`, `pivot`, `pivotReferenceSize`, `worldSpaceSize`, and
  `worldSpaceSizeMode`. Unity 6 renders the panel in world space natively — no render texture, no intermediate
  quad, no cloning. This is why `UiRenderTextureFeature` (below) is uGUI-only.

## `UiCameraStackFeature`

Lets a presenter's `Canvas` render as Screen Space - Camera and stack, via URP camera stacking, onto a base
camera — so UI can layer over 3D content with correct depth sorting instead of always compositing on top of
everything the way Screen Space - Overlay does.

Requires two prefab-child references, assigned in the Inspector:
- An overlay **Camera** (a child GameObject under the presenter) — this feature disables it and drives it
  entirely via the stack.
- The presenter's own **Canvas** — switched to Screen Space - Camera automatically.

Base-camera resolution goes through `IUiBaseCameraProvider` (default: `UiBaseCameraProvider`, wrapping
`Camera.main`, cached and invalidated on scene load). Swap it via `SetBaseCameraProvider(...)` if `Camera.main`
isn't the right answer in your game (multiple cameras, no "MainCamera" tag, split-screen).

Multiple presenters stacking onto the same base camera are ordered by `Canvas.sortingOrder` (or an explicit
`stackPriority` if `_useCanvasSortingOrder` is off), via `UiCameraStackRegistry` — a pure insert-index function
with zero engine references.

### ⚠️ `UiConfig.Layer` is not a total order once render modes mix

Screen Space - Overlay canvases composite after **all** URP rendering, straight to the backbuffer. A stacked
Screen Space - Camera presenter at a high `Layer` still draws **underneath** an Overlay presenter at `Layer` 0,
the moment both exist in the same scene. This is a fundamental property of how the two render modes composite
— there is no runtime fix. Keep Overlay and camera-stacked presenters visually separated by design (e.g. HUD as
Overlay, world-anchored UI as stacked), or accept the fixed relative order between the two groups.

## `UiRenderTextureFeature`

Renders a presenter's **uGUI `Canvas`** into a `RenderTexture` instead of the screen — for 3D-in-world UI,
portals, or minimap-style composition.

**uGUI only, deliberately.** For UI Toolkit, use `PanelRenderMode.WorldSpace` (see above) — it renders a
`UIDocument` in world space natively, with no texture round-trip and no shared-asset mutation. Adding this
feature to a presenter that has a `UIDocument` and no `Canvas` logs an error naming that alternative.

Screen Space - Overlay canvases cannot render into a `RenderTexture`, so this feature switches the canvas to
Screen Space - Camera and points a dedicated render `Camera` (assign one in the Inspector) at the texture.
Without that camera assigned, it logs an error rather than silently doing nothing.

Defaults to a pre-authored `RenderTexture` asset (assign one in the Inspector) — no lifetime or
resolution-change problem. Dynamic allocation is the escape hatch (`_matchScreenSize` reallocates on
`UiService.OnResolutionChanged`), tracked via an ownership flag so a preset asset is never released or
destroyed by this feature.

**Depth/stencil is not optional**: uGUI `Mask`/`RectMask2D` needs a stencil buffer. The default depth bits (24)
allocates depth+stencil.

**Color space is an open question, not a settled one**: in a Linear project, UI rendered to a texture and then
sampled by a world-space material can double-apply or drop the sRGB transform, depending on the sampling
material's own handling. This has not been verified against a real rendered frame — check visually before
shipping a world-space consumer of this feature's output.

### ⚠️ Mutually exclusive with `UiCameraStackFeature`

URP requires a stacked overlay camera to share the base camera's render target; a render-texture-target camera
by definition renders to its own texture. Do not add both features to the same presenter.

## `UiBackdropBlurFeature` + `UiBackdropBlurPresenterFeature`

Blurs everything behind a presenter's UI while it's open — a translucent, blurred backdrop behind a modal.

Two pieces, because a `ScriptableRendererFeature` is a shared asset instance across all cameras, separate from
any one presenter:

1. **`UiBackdropBlurFeature`** — add this once to your project's active URP Renderer asset (Inspector: Renderer
   Features list). Ships its own shader (`Shaders/UiBackdropBlur.shader`) via a serialized `Shader` reference,
   with an Editor-only auto-load fallback so a fresh install doesn't need a manual drag-and-drop.
2. **`UiBackdropBlurPresenterFeature`** — add this to any presenter that wants a blurred backdrop while open.
   Configurable: downsample factor, blur iterations, spread, tint (e.g. a translucent dark tint), and a sort key
   for resolving which request wins when multiple presenters request a blur simultaneously (highest sort key,
   first-registered wins ties).

Because every canvas in this package's samples is Screen Space - Overlay (which composites after all URP
rendering), blurring the camera's own color target in place — before post-processing — blurs everything behind
the UI with **zero canvas changes**. That's the whole design: no backdrop element to wire up, no extra Canvas
work, on any presenter that adds `UiBackdropBlurPresenterFeature`.

The renderer feature only actually injects the blur pass when all of these hold (`UiBackdropBlurInjection.ShouldInject`):
- the camera is a Game camera (not Scene view / Preview — otherwise the Editor's own Scene view and material
  preview thumbnails would get blurred too)
- the camera is **not** a `UiCameraStackFeature` overlay camera (blurring that would blur the UI itself, since
  renderer features run per camera in a stack and post-processing-style passes run on every camera in it)
- the camera does **not** have a `RenderTexture` target via `UiRenderTextureFeature` (blurring that blurs the UI
  being captured, not the world behind it)
- at least one presenter currently has an active blur request

### Known coverage gap: visual correctness is not automatically tested

The shader itself is verified to compile (via `ShaderUtil.ShaderHasError` against a real Material instance) and
the injection/registration logic has full automated coverage, but whether the blur actually **looks** right —
the correct blend of downsample factor, iterations, and spread for your content — has not been verified against
a real rendered frame in this repo. Check it visually in the Editor Game view before shipping.
