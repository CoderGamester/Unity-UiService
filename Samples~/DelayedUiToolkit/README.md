# Delayed UI Toolkit Sample

> **Unity compatibility:** Minimum Unity version `6000.0`; reference streams are `6000.0.x`, `6000.3.x`, and `6000.5.x`. UI Service is **URP-only**. See the [package compatibility matrix](../../README.md#unity-compatibility).

This sample demonstrates combining multiple features: delay transitions with UI Toolkit integration.

## Design Philosophy

The UI Service uses feature composition to combine capabilities without inheritance conflicts:
1. **Multiple features**: Attach `TimeDelayFeature` + `UiToolkitPresenterFeature` to the same presenter.
2. **Animation + Data**: Combine `AnimationDelayFeature` with `UiPresenter<T>` for data-driven animated UI.
3. **Transition awareness**: Disable interaction during transitions, enable after completion.

## Sample Content

### Time-Delayed UI Toolkit Presenter
Combines `TimeDelayFeature` with `UiToolkitPresenterFeature`. Disables UI during open transition.

### Animation-Delayed Data Presenter
Combines `AnimationDelayFeature` with `UiPresenter<T>` and `UiToolkitPresenterFeature`. Shows data-driven content with animated transitions.

## How to Use

1. **Import the sample** and open the `DelayedUiToolkit.unity` scene.
2. **Enter Play Mode** to see multi-feature composition in action.
3. **Interact with the buttons**:
   - **Open Time Delayed**: Opens UI Toolkit presenter with time delay.
   - **Open Animated**: Opens data-driven UI Toolkit presenter with animation.

## Implementation Details

### Feature Composition
`TimeDelayedUiToolkitPresenter.cs` uses both features via `[RequireComponent]` attributes:
- Disables root in `OnOpened()`.
- Enables root in `OnOpenTransitionCompleted()`.

### Data + Animation
`AnimationDelayedUiToolkitPresenter.cs` extends `UiPresenter<UiToolkitExampleData>` and combines animation with data binding.
