# URP Rendering Features

Two presenter features for Universal Render Pipeline projects, under `Runtime/Rendering/` (namespace
`GameLovers.UiService.Rendering`). This package depends on `com.unity.render-pipelines.universal` (17.0.1+) —
it is a **URP-only package**.

This page covers only what you can't discover from the Inspector: the required setup step for the blur, and
the hazards. For the general feature-composition pattern see [Core Concepts](core-concepts.md).

## `UiCameraStackFeature`

Renders a presenter's `Canvas` as Screen Space - Camera and inserts its overlay camera into a base camera's
URP stack, so UI can layer over 3D content instead of always compositing on top.

Assign the overlay **Camera** (a prefab child) and the presenter's **Canvas** in the Inspector. The base camera
defaults to `Camera.main`; assign `BaseCameraResolver` (a `Func<Camera>`) if that's not right for your game —
multiple cameras, no "MainCamera" tag, split-screen.

**Why this is a runtime feature and not authoring**: URP's camera stack is an Inspector list on the *base*
camera in the scene. Presenters are instantiated from prefabs at runtime, so they cannot be pre-authored into
that list — the feature inserts on open and removes on disable.

## `UiBackdropBlurRendererFeature` + `UiBackdropBlurPresenterFeature`

Blurs everything behind a presenter's UI while it's open.

> ### ⚠️ Required setup: add `UiBackdropBlurRendererFeature` to your URP Renderer asset
>
> Select your Universal Renderer asset (e.g. `Assets/Settings/URP-Mobile_Renderer.asset`) → **Add Renderer
> Feature** → `UiBackdropBlurRendererFeature`. **Until you do this, the blur silently does nothing** — nothing in the
> API or Inspector will tell you it's missing. This is the single most common reason the blur "doesn't work".

Then add `UiBackdropBlurPresenterFeature` to any presenter that wants a blurred backdrop. Configurable:
downsample factor, iterations, spread, tint, and a sort key resolving which request wins when several
presenters request a blur at once (highest sort key, first-registered breaks ties).

The pass injects only when all of these hold (`UiBackdropBlurRequests.ShouldInject`):

- the camera is a **Game** camera — otherwise the Editor's Scene view and material preview thumbnails would
  get blurred too
- the camera is **not** a `UiCameraStackFeature` overlay camera — renderer features run per camera in a stack,
  so injecting there would blur the UI itself rather than the world behind it
- the camera has **no `RenderTexture` target** — blurring that blurs the UI being captured
- at least one presenter currently has an active blur request

## Rendering UI into a RenderTexture

There is no feature for this, because Unity 6 makes it pure authoring — no code:

| UI system | Setup |
|---|---|
| **uGUI** | `Canvas` → Render Mode = Screen Space - Camera → assign a camera → set that **camera's Target Texture** |
| **UI Toolkit** | `PanelSettings` → **Render Texture** |

**Depth/stencil is not optional** for the uGUI path: `Mask`/`RectMask2D` needs a stencil buffer, so create the
`RenderTexture` with 24-bit depth (which allocates depth+stencil), not 0 or 16.

**Color space is an open question**: in a Linear project, UI rendered to a texture and then sampled by a
world-space material can double-apply or drop the sRGB transform, depending on the sampling material. Check
visually.

For **world-space UI Toolkit**, prefer `PanelSettings.renderMode = PanelRenderMode.WorldSpace` over a texture
entirely — Unity renders the panel in world space natively, positioned per-document via `UIDocument.position`,
`pivot`, `pivotReferenceSize`, `worldSpaceSize`, and `worldSpaceSizeMode`.

## ⚠️ `UiConfig.Layer` is not a total order once render modes mix

Screen Space - Overlay canvases composite after **all** URP rendering, straight to the backbuffer. So a stacked
Screen Space - Camera presenter at a high `Layer` still draws **underneath** an Overlay presenter at `Layer` 0,
the moment both exist in the same scene.

This is how the two render modes composite — **there is no runtime fix.** Keep Overlay and camera-stacked
presenters visually separated by design (e.g. HUD as Overlay, world-anchored UI as stacked), or accept the
fixed relative order between the two groups.

Related: don't point a `UiCameraStackFeature` presenter's camera at a `RenderTexture`. URP requires a stacked
overlay camera to share the base camera's target; a render-texture camera renders to its own. Pick one.

## ⚠️ Never mutate `PanelSettings` at runtime

A `PanelSettings` asset *is* a panel. Every `UIDocument` pointing at the same asset shares one panel — which is
what makes cross-presenter ordering via `UIDocument.sortingOrder` (set by `UiService` from `UiConfig.Layer`)
work, and what lets them batch. Writing to the asset reconfigures every other presenter using it, and in the
Editor persists to disk.

If a presenter needs different panel settings, **author a second `PanelSettings` asset** and point its
`UIDocument` at it. Group presenters by panel configuration; you want as few panels as possible.
`UiToolkitPresenterFeature.PanelSettings` is exposed read-only for inspection.

## Known coverage gap: blur visual correctness is not automatically tested

The shader is verified to compile (`ShaderUtil.ShaderHasError` against a real Material) and the
injection/registration logic has full automated coverage. Whether the blur **looks** right — the right
downsample/iteration/spread blend for your content — has not been verified against a real rendered frame in
this repo. Check it in the Game view before shipping.
