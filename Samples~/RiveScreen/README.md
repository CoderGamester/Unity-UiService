# Rive Screen UI

1. Create `Rive > Rive Panel (Canvas)` under a screen-space Canvas.
2. Add `RiveLoadingSamplePresenter`, `RiveUiSurface`, `RiveRenderTargetFeature`, and
   `RiveViewModelBinding` to the panel root.
3. Assign a Rive asset whose default view model exposes a number property named `progress`.
4. Declare `progress` in the binding so the inspector validates the path.

Keep one panel per screen or larger UI region. Multiple widgets under that panel share one render
texture; per-control panels defeat Rive's batching model.
