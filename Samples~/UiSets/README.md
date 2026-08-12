# UI Sets Sample

> **Unity compatibility:** Minimum Unity version `6000.0`; reference streams are `6000.0.x`, `6000.3.x`, and `6000.5.x`. UI Service is **URP-only**. See the [package compatibility matrix](../../README.md#unity-compatibility).

This sample demonstrates grouping multiple UIs for simultaneous management, commonly used for HUD elements.

## Design Philosophy

Games often have multiple UI elements that need to be shown/hidden together (health bar, currency, minimap). UI Sets provide:
1. **Grouped management**: Load, open, close, or unload all UIs in a set with a single call.
2. **Preloading**: Load all HUD elements before gameplay starts to avoid hitches.
3. **Clean transitions**: Hide all HUD elements when entering a menu, restore when returning.

## Sample Content

### Game HUD Set
Contains two HUD elements:
- **HudHealthBarPresenter**: Displays player health.
- **HudCurrencyPresenter**: Displays player currency.

## How to Use

1. **Import the sample** and open the `UiSets.unity` scene.
2. **Enter Play Mode** to see UI Sets in action.
3. **Interact with the buttons**:
   - **Load Set**: Loads all UIs in the set (not visible yet).
   - **Open Set**: Opens all UIs in the set (loads if needed).
   - **Close Set**: Hides all UIs but keeps them in memory.
   - **Unload Set**: Destroys all UIs in the set.
   - **List Sets**: Logs configured UI Sets to the console.

## Implementation Details

### Set Configuration
UI Sets are configured in `UiSetsConfigs.asset`. Each set contains a list of presenter types.

### Set ID
`UiSetId.cs` defines an enum for type-safe set identification. The sample uses `UiSetId.GameHud`.

### Custom Editor
`UiSetsConfigsEditor.cs` in the `Editor/` folder shows how to create a custom inspector for set configuration.
