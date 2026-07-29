# URP Rendering Sample

Demonstrates the two URP-only presenter features and the one layering hazard they introduce.

Open `UrpRendering.unity` and press Play. Three spinning cubes give you something to render UI against.

| Button | What it shows |
|---|---|
| **Open Blurred Modal** | `UiBackdropBlurPresenterFeature` — the world behind the panel is blurred |
| **Open Stacked UI** | `UiCameraStackFeature` — a Screen Space - Camera presenter stacked into the URP frame |
| **Open Overlay HUD (Layer 0)** | The layering hazard, below |
| **Close All** | Closes everything; the blur lifts when the last blur presenter hides |

## Setup is automatic

`UiBackdropBlurRendererFeature` has to be on your URP Renderer asset or the blur does nothing. The sample
installs it for you on import — `Editor/UrpRenderingSampleSetup.cs` adds it to every Renderer asset your
active URP pipeline uses, and it's idempotent, so re-imports are silent no-ops.

**Manual fallback** if it didn't run: **Tools → GameLovers → Samples → Urp Rendering → Add Backdrop Blur
Renderer Feature**. Or do it by hand: select your Universal **Renderer** asset (not the Pipeline asset) →
**Add Renderer Feature** → `UiBackdropBlurRendererFeature`.

**To undo**: remove the feature from the Renderer asset. The sample never touches anything else in your
project settings.

## The layering hazard this sample exists to show

Open the **Stacked UI**, then open the **Overlay HUD** on top of it. The configured layers are:

| Presenter | `UiConfig.Layer` | Render mode |
|---|---|---|
| Overlay HUD | **0** (lowest) | Screen Space - Overlay |
| Blurred Modal | 10 | Screen Space - Overlay |
| Stacked UI | **20** (highest) | Screen Space - Camera |

The Overlay HUD is at the *lowest* layer and still draws **over** the stacked presenter at the highest.
That's not a bug and there's no runtime fix: Screen Space - Overlay canvases composite after all URP
rendering, straight to the backbuffer, so they always land on top of anything drawn inside the frame.

Keep Overlay and camera-stacked presenters visually separated by design — HUD as Overlay, world-anchored UI
as stacked — or accept the fixed relative order between the two groups.

## Tuning the blur

The look is authored on the **renderer feature**, not per presenter — it's one art decision for a project.
Select your Renderer asset and adjust `Downsample`, `Iterations`, `Spread`, and `Tint` on the
`UiBackdropBlurRendererFeature` entry. Every blurred presenter shares those values.

`UiBackdropBlurPresenterFeature` on a prefab has no settings at all; it just holds the blur open while that
presenter is visible.

## Files

```
UrpRendering.unity                  # Scene: base camera, 3 spinning cubes, driver canvas
UrpRenderingCanvas.prefab           # Driver UI (buttons + log pane)
UrpRenderingExample.cs              # Driver: opens presenters, spins the cubes
UrpRenderingConfigs.asset           # PrefabRegistryUiConfigs — no Addressables needed
BlurredModalPresenter.cs/.prefab    # + UiBackdropBlurPresenterFeature
StackedWorldPresenter.cs/.prefab    # + UiCameraStackFeature and a child overlay Camera
OverlayHudPresenter.cs/.prefab      # Plain Overlay, Layer 0, for the hazard demo
Editor/UrpRenderingSampleSetup.cs   # Installs the renderer feature on import
```

This sample uses `PrefabRegistryUiConfigs`, so it needs no Addressables setup.

## Related docs

[URP Rendering Features](../../docs/urp-rendering.md) — full gating rules, hazards, and the render-texture
authoring recipe.
