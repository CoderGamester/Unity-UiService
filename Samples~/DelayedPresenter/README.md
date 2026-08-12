# Delayed Presenter Sample

> **Unity compatibility:** Minimum Unity version `6000.0`; reference streams are `6000.0.x`, `6000.3.x`, and `6000.5.x`. UI Service is **URP-only**. See the [package compatibility matrix](../../README.md#unity-compatibility).

This sample demonstrates transition delays using `TimeDelayFeature` and `AnimationDelayFeature`.

## Design Philosophy

UI transitions enhance user experience by providing visual feedback. The UI Service uses composable features:
1. **TimeDelayFeature**: Simple time-based delays for open/close transitions.
2. **AnimationDelayFeature**: Synchronizes with Unity Animation clips.
3. **Lifecycle hooks**: `OnOpenTransitionCompleted()` and `OnCloseTransitionCompleted()` fire after transitions finish.

## Sample Content

### Time-Delayed Presenter
Uses `TimeDelayFeature` with configurable open/close delay durations.

### Animation-Delayed Presenter
Uses `AnimationDelayFeature` with intro/outro animation clips.

## How to Use

1. **Import the sample** and open the `DelayedPresenter.unity` scene.
2. **Enter Play Mode** to see the delayed transitions in action.
3. **Interact with the buttons**:
   - **Open Time Delayed**: Opens UI with a time-based delay.
   - **Open Animated**: Opens UI with animation-based delay.

## Implementation Details

### Time Delay
`DelayedUiExamplePresenter.cs` uses `[RequireComponent(typeof(TimeDelayFeature))]` and configures delay durations in the Inspector.

### Animation Delay
`AnimatedUiExamplePresenter.cs` uses `[RequireComponent(typeof(AnimationDelayFeature))]` with Animation component and intro/outro clips.

### Transition Hooks
Override `OnOpenTransitionCompleted()` to react when the open transition finishes (e.g., enable interaction).
