# Rive World UI

1. Create a standalone `Rive Panel` and add `RiveWorldSamplePresenter`,
   `RiveRenderTargetFeature`, and `RiveViewModelBinding`.
2. Put `RiveTextureRenderer` and `RiveUiSurface` on a quad with a `MeshCollider`.
3. Set the surface space to `World`, and use `SharedAtlas` with the same non-empty group id for all
   repeated surfaces.
4. Keep every panel in that atlas on `DrawWhenChanged`. One `AlwaysDraw` panel disables native dirt
   checking for the entire atlas.
5. Add `UiDistanceVisibilityFeature` to stop panel rendering while its mesh is out of range.

Interactive world surfaces also require an EventSystem, a PhysicsRaycaster on the event camera, and
a MeshCollider on the displayed mesh.
