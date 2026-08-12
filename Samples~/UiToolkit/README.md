# UI Toolkit Sample

> **Unity compatibility:** Minimum Unity version `6000.0`; reference streams are `6000.0.x`, `6000.3.x`, and `6000.5.x`. UI Service is **URP-only**. See the [package compatibility matrix](../../README.md#unity-compatibility).

This sample demonstrates UI Toolkit integration with the UI Service using `UiToolkitPresenterFeature`.

## Design Philosophy

UI Toolkit provides a modern, CSS-like approach to UI development. This sample shows:
1. **UiToolkitPresenterFeature**: Provides access to the `UIDocument.rootVisualElement`.
2. **Element queries**: Find elements by name using `root.Q<T>("element-name")`.
3. **Event binding**: Register callbacks for UI Toolkit events.

## How to Use

1. **Import the sample** and open the `UiToolkit.unity` scene.
2. **Enter Play Mode** to see UI Toolkit integration in action.
3. **Interact with the buttons**:
   - **Load**: Loads the UI Toolkit presenter into memory.
   - **Open**: Makes the presenter visible.
   - **Unload**: Destroys the presenter.

## Implementation Details

### Presenter
`UiToolkitExamplePresenter.cs` uses `[RequireComponent(typeof(UiToolkitPresenterFeature))]` and queries elements in `OnInitialized()`:
- Access root via `_toolkitFeature.Root`.
- Query elements via `root.Q<Button>("button-name")`.
- Bind events via `button.clicked += OnButtonClicked`.

### UI Layout
`UiToolkitExample.uxml` defines the visual tree structure.
`UiToolkitControls.uxml` provides the sample's control panel.
