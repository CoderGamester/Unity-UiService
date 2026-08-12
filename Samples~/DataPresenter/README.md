# Data Presenter Sample

> **Unity compatibility:** Minimum Unity version `6000.0`; reference streams are `6000.0.x`, `6000.3.x`, and `6000.5.x`. UI Service is **URP-only**. See the [package compatibility matrix](../../README.md#unity-compatibility).

This sample demonstrates data-driven UI presenters using `UiPresenter<T>` for typed data binding.

## Design Philosophy

UI often needs to display dynamic data. This sample shows two patterns:
1. **Initial data**: Pass data when opening the UI via `OpenUiAsync<T, TData>(data)`.
2. **Dynamic updates**: Update data at any time via the `Data` property, which triggers `OnSetData()` automatically.

## Sample Content

The sample displays player character information with different stats:

### Character Data
- **Warrior**: High level, good health, moderate score.
- **Mage**: Max level, medium health, high score.
- **Rogue**: Mid level, full health, lower score.

### Dynamic Update
The "Update Low Health" button demonstrates updating data on an already-open presenter.

## How to Use

1. **Import the sample** and open the `DataPresenter.unity` scene.
2. **Enter Play Mode** to see the data-driven UI in action.
3. **Interact with the buttons**:
   - **Show Warrior/Mage/Rogue**: Opens the UI with different character data.
   - **Update Low Health**: Changes the health value on the open UI.

## Implementation Details

### Data Structure
`PlayerData` struct contains: `PlayerName`, `Level`, `Score`, `HealthPercentage`.

### Presenter
`DataUiExamplePresenter.cs` extends `UiPresenter<PlayerData>` and implements:
- `OnSetData()`: Called whenever `Data` is assigned (initial or update).
