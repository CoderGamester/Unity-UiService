# Multi-Instance Sample

> **Unity compatibility:** Minimum Unity version `6000.0`; reference streams are `6000.0.x`, `6000.3.x`, and `6000.5.x`. UI Service is **URP-only**. See the [package compatibility matrix](../../README.md#unity-compatibility).

This sample demonstrates creating multiple instances of the same UI type, commonly used for popups and notifications.

## Design Philosophy

Some UIs need multiple simultaneous instances (notification popups, chat bubbles, tooltips). Multi-instance support provides:
1. **Instance addresses**: Unique string identifiers for each instance.
2. **Independent lifecycle**: Each instance can be loaded, opened, closed, and unloaded independently.
3. **Self-closing**: Presenters can close themselves using their stored instance address.

## Sample Content

### Notification Popups
Creates multiple popup instances with unique addresses like `popup_1`, `popup_2`, etc.

## How to Use

1. **Import the sample** and open the `MultiInstance.unity` scene.
2. **Enter Play Mode** to see multi-instance popups in action.
3. **Interact with the buttons**:
   - **Spawn Popup**: Creates a new popup with a unique instance address.
   - **Close Recent**: Closes the most recently opened popup.
   - **Close All**: Closes all active popups.
   - **List Active**: Logs all active popup instances to the console.

## Implementation Details

### Instance Address
Each popup is loaded with a unique address: `$"popup_{_counter}"`.

### UiInstanceId
Combines `Type` + `InstanceAddress` for unique identification.

### Self-Close
`NotificationPopupPresenter.cs` calls `Close(destroy: true)` which uses the presenter's stored instance address to unload the correct instance.
