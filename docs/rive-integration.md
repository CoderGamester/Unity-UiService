# Rive integration

UiService keeps Rive optional. Installing `app.rive.rive-unity` 0.4.3 or newer enables the
`GameLovers.UiService.Rive` assembly; projects without Rive compile only the core package.

## Screen-space UI

Use a normal UiService presenter prefab under a uGUI Canvas:

- One `RivePanel` for the screen or a large UI region.
- One or more child `RiveWidget` components.
- `RiveCanvasRenderer` for display and EventSystem input.
- `RiveUiSurface` for UiService ordering.
- `RiveViewModelBinding` for cached data-binding writes.

Prefer a complete menu or screen in one Rive widget. Creating one panel per button allocates one
render texture per control and defeats Rive's recommended composition model.

## World-space UI

World-space Rive renders a panel texture on a mesh:

- Keep the `RivePanel` under the presenter's managed hierarchy.
- Display it with `RiveTextureRenderer` on a quad or other mesh.
- Add a `MeshCollider` when the surface is interactive.
- Add a PhysicsRaycaster to the input camera.
- Configure `RiveUiSurface` as `World`.

Repeated world surfaces should use one `SharedAtlas` group. `RiveRenderTargetFeature` reference-counts
the atlas and releases it after the last presenter unloads. Every panel in an atlas must use
`DrawWhenChanged`; an `AlwaysDraw` panel forces the entire atlas to redraw and disables native
artboard dirt checking.

Use pooled targets for frequently appearing surfaces that cannot share one atlas size. Dedicated
targets are appropriate only for a small number of unique, high-resolution surfaces.

## Lifecycle and ownership

`RiveWidget.Load(Rive.Asset)` owns the decoded file. If an imported Rive asset is loaded through
Addressables, use `RiveAddressableFileLease`: it disposes the decoded `Rive.File` before releasing
the Addressables handle.

`RiveViewModelBinding` subscribes while its presenter is open, caches typed paths, and clears those
references on close. Declare expected property paths in the component so the custom inspector can
validate the imported artboard before Play mode.

`UiDistanceVisibilityFeature` changes render activity through `IUiRenderActivity`; it never
deactivates the presenter. `UiPresenter` remains the only owner of GameObject visibility after close
transitions.

## URP requirements

Rive 0.4.3 on Unity 6000.4 or newer implements only `RecordRenderGraph`. URP Compatibility Mode
(Render Graph disabled) therefore produces blank Rive render targets and must remain off.

Rive's URP handler is created automatically. Do not install a separate renderer feature.

Avoid Screen Space Camera presentation for Rive output unless the camera order has been validated:
Rive updates its render texture at `AfterRenderingTransparents`, so sampling it from an earlier
camera can display the previous frame. Screen Space Overlay is the default screen-space path.

## Input ownership

Pass an `EventSystemUiInputRouter` to the UiService constructor when the package should establish
input prerequisites. The consumer supplies an input-module installer because only the application
knows whether Legacy Input or the Input System package is authoritative. The router adds missing
GraphicRaycaster and PhysicsRaycaster components, and reports a precise error for a missing input
module, world camera, or mesh collider.
