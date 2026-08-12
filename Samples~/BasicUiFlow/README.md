# Basic UI Flow Sample

> **Unity compatibility:** Minimum Unity version `6000.0`; reference streams are `6000.0.x`, `6000.3.x`, and `6000.5.x`. UI Service is **URP-only**. See the [package compatibility matrix](../../README.md#unity-compatibility).

This sample demonstrates the core UI presenter lifecycle: loading, opening, closing, and unloading.

## Design Philosophy

The UI Service separates the concept of **loading** (instantiating into memory) from **opening** (making visible). This allows:
1. **Preloading**: Load UI into memory before it's needed to avoid hitches.
2. **Reusability**: Keep UI in memory after closing for fast reopening.
3. **Clean lifecycle**: Clear separation of resource management and visibility.

## How to Use

1. **Import the sample** and open the `BasicUiFlow.unity` scene.
2. **Enter Play Mode** to see the UI service in action.
3. **Interact with the buttons**:
   - **Load**: Loads the UI into memory (not visible yet).
   - **Open**: Makes the UI visible.
   - **Close**: Hides the UI but keeps it in memory.
   - **Unload**: Destroys the UI and removes it from memory.
   - **Load & Open**: Loads and opens in one call.

## Implementation Details

### Presenter
`BasicUiExamplePresenter.cs` extends `UiPresenter` and implements lifecycle hooks:
- `OnInitialized()`: Called once when the presenter is first loaded.
- `OnOpened()`: Called each time the presenter becomes visible.
- `OnClosed()`: Called each time the presenter is hidden.

### Scene Setup
`BasicUiFlowExample.cs` initializes the UI service with a `PrefabRegistryUiConfigs` asset and wires button events to service calls.
