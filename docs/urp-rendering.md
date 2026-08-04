# URP Rendering Features

Two presenter features for Universal Render Pipeline projects, under `Runtime/Rendering/` (namespace
`GameLovers.UiService.Rendering`). This package depends on `com.unity.render-pipelines.universal` (17.0.1+) —
it is a **URP-only package**.

This page covers only what you can't discover from the Inspector: the required setup step for the blur, and
the hazards. For the general feature-composition pattern see [Core Concepts](core-concepts.md).

## `UiCameraStackFeature`

Renders a presenter's `Canvas` as Screen Space - Camera and inserts its overlay camera into a base camera's
URP stack, so UI can layer over 3D content instead of always compositing on top.

Assign the overlay **Camera** and the presenter's **Canvas** in the Inspector. Authoring the camera as a child
of the presenter prefab is fine: the feature re-parents it out of the canvas hierarchy on init, because a
Screen Space - Camera canvas drives its own transform from its `worldCamera` and would otherwise drag the
camera onto the canvas plane where it can see nothing. Having done that, the feature destroys the camera with
the presenter.

The base camera defaults to `Camera.main`; assign `BaseCameraResolver` (a `Func<Camera>`) if that's not right
for your game — multiple cameras, no "MainCamera" tag, split-screen.

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

Tune the look — downsample, iterations, spread, tint — **on that renderer feature**. It is one art decision
for the project, so it is not configurable per presenter; every blurred presenter shares it.

For runtime tuning (quality tiers, an accessibility slider, a settings menu) use
`UiBackdropBlurRendererFeature.IterationsOverride`, a static non-serialized override clamped to
`MinIterations`..`MaxIterations`; zero or less falls back to the authored value, and `EffectiveIterations`
reports what is actually in effect. It exists instead of writing to the asset's fields because the feature is a
`ScriptableObject` — a runtime write would persist to disk in the Editor and change the authored look
project-wide. Iterations is the most intuitive strength knob but the most expensive (one full-screen draw
each); `Downsample` buys more blur for less.

Then add `UiBackdropBlurPresenterFeature` to any presenter that wants a blurred backdrop. It has no settings:
it just holds the blur open while the presenter is visible, and the blur stays up until the last such
presenter closes. Opening one with no renderer feature installed logs an error naming the setup step above.

The pass injects only when all of these hold (`UiBackdropBlurPass.ShouldInject`):

- the camera is a **Game** camera — otherwise the Editor's Scene view and material preview thumbnails would
  get blurred too
- the camera is **not** a `UiCameraStackFeature` overlay camera — renderer features run per camera in a stack,
  so injecting there would blur the UI itself rather than the world behind it
- the camera has **no `RenderTexture` target** — blurring that blurs the UI being captured
- at least one `UiBackdropBlurPresenterFeature` presenter is currently open

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

## Two benign Metal warnings while the blur is active

On Metal, URP keeps the camera depth attachment memoryless. Injecting a full-screen pass breaks the native
render pass, so the backend logs:

```
Ignoring depth surface load action as it is memoryless
Ignoring depth surface store action as it is memoryless
```

Both come from Unity's native renderer rather than this package, are emitted once per session, and say Unity
ignored the action. They stop as soon as no presenter is requesting a blur. Setting
`requiresIntermediateTexture` and stripping depth from the blur work textures does not silence them.

## Coverage: verified by eye, not automatically

The shader compiles (`ShaderUtil.ShaderHasError` against a real Material), and the injection matrix plus the
open/close refcount have full automated coverage. The blur has also been **confirmed rendering** in the
`UrpRendering` sample — captured from the Game view in play mode, with the 3D content visibly softened behind
the modal and crisp again once it closes.

What is still not automated is whether it looks *good*: the right downsample/iteration/spread blend is a
judgement call for your content and resolution. Adjust those on the renderer feature and look at the result.
