# URP Rendering Features

Four presenter features for Universal Render Pipeline projects, all living under `Runtime/Rendering/` in a
dedicated assembly (`GameLovers.UiService.Urp`, namespace `GameLovers.UiService.Rendering`). This package
depends on `com.unity.render-pipelines.universal` (17.0.1+) for exactly these features — nothing else in the
package touches URP.

Add any of these as components on a presenter prefab alongside its `UiPresenter` subclass, the same way you'd
add `TimeDelayFeature` or `UiToolkitPresenterFeature` — see [Core Concepts](core-concepts.md) for the general
feature-composition pattern.

## `UiPanelSettingsFeature`

Gives a UI Toolkit presenter (one with `UiToolkitPresenterFeature`) safe, per-instance control over its
`PanelSettings` — render mode, clear color, resolution — without mutating the shared asset other presenters
may reference.

```csharp
[RequireComponent(typeof(UiToolkitPresenterFeature))]
```

`PanelSettings` is a plain `ScriptableObject` asset, commonly shared across every UI Toolkit presenter in a
project. Writing directly to a shared asset's fields mutates it for every consumer, and in the Editor persists
that mutation to disk. This feature clones per-presenter instead, controlled by `UiPanelSettingsClonePolicy`:

| Policy | Behavior |
|---|---|
| `UseSharedAsset` | Never clones. Overrides are ignored (logged) since applying them would mutate the shared asset. |
| `CloneWhenOverridden` (default) | Clones only if at least one `UiPanelSettingsOverrides` field is set, or `SetTargetTexture` is called. |
| `CloneAlways` | Always clones on init. |

`ActivePanelSettings` returns whichever is currently in effect (clone or shared asset); `IsClone` reports which.
`SetTargetTexture(RenderTexture)` is the seam `UiRenderTextureFeature`'s UI Toolkit path uses — call it directly
only if you're not also using that feature.

**Layering note**: multiple `UIDocument`s sharing one `PanelSettings` share one panel, ordered via
`UIDocument.sortingOrder` (what `UiService` sets from `UiConfig.Layer`). Once a presenter clones its
`PanelSettings`, it gets its own panel, and its ordering switches to `PanelSettings.sortingOrder`. This feature
mirrors `Document.sortingOrder` onto the clone automatically — you don't need to do anything, but if you're
debugging a layering issue on a UI Toolkit presenter, check `IsClone` first.

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

Renders a presenter's UI into a `RenderTexture` instead of the screen — for 3D-in-world UI, portals, or
minimap-style composition.

Resolves `UiRenderTextureMode.Auto` from the presenter's own components (`UiPanelSettingsFeature` present →
`.UiToolkit`, otherwise → `.Canvas`), or set the mode explicitly.

- **Canvas mode**: Screen Space - Overlay canvases cannot render into a `RenderTexture`. This feature switches
  to Screen Space - Camera and points a dedicated render `Camera` (assign one in the Inspector) at the texture.
  Without that camera assigned, it logs an error rather than silently doing nothing.
- **UiToolkit mode**: routes through `UiPanelSettingsFeature.SetTargetTexture(...)` — requires that feature on
  the same GameObject. Never assign `PanelSettings.targetTexture` directly; there must be exactly one owner of
  the clone.

Defaults to a pre-authored `RenderTexture` asset (assign one in the Inspector) — no lifetime or
resolution-change problem. Dynamic allocation is the escape hatch (`_matchScreenSize` reallocates on
`UiService.OnResolutionChanged`), tracked via an ownership flag so a preset asset is never released or
destroyed by this feature.

**Depth/stencil is not optional**: both uGUI `Mask`/`RectMask2D` and UI Toolkit masking need a stencil buffer.
The default depth bits (24) allocates depth+stencil.

**Color space is an open question, not a settled one**: in a Linear project, UI rendered to a texture and then
sampled by a world-space material can double-apply or drop the sRGB transform, and the correct combination is
expected to differ between the uGUI and UI Toolkit paths. This has not been verified against a real rendered
frame — check visually before shipping a world-space consumer of this feature's output.

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
