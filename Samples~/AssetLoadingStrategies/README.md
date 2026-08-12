# Asset Loading Strategies Sample

> **Unity compatibility:** Minimum Unity version `6000.0`; reference streams are `6000.0.x`, `6000.3.x`, and `6000.5.x`. UI Service is **URP-only**. See the [package compatibility matrix](../../README.md#unity-compatibility).

This sample demonstrates different UI asset loading strategies: PrefabRegistry, Addressables, and Resources.

## Design Philosophy

Different projects have different asset management needs. The UI Service supports multiple loading strategies:
1. **PrefabRegistry**: Direct prefab references in the config. Best for samples and small projects.
2. **Resources**: Loads from the Resources folder. Simple but limited.
3. **Addressables**: Full asset management with remote loading support. Best for production.

## Sample Content

### Loading Strategies
- **PrefabRegistry**: Works immediately after import. No setup required.
- **Resources**: Works immediately. Uses `Resources/ExamplePresenter.prefab`.
- **Addressables**: Requires setup (see below).

### Runtime Switching
The dropdown allows switching strategies at runtime to compare behavior.

## How to Use

1. **Import the sample** and open the `AssetLoadingStrategies.unity` scene.
2. **Enter Play Mode** to test different strategies.
3. **Select a strategy** from the dropdown.
4. **Interact with the buttons**:
   - **Load**: Loads the UI using the selected strategy.
   - **Open**: Opens the loaded UI.
   - **Unload**: Destroys the UI.

## Addressables Setup

To use the Addressables strategy:
1. Open **Window > Asset Management > Addressables > Groups**.
2. If no groups exist, click **Create Addressables Settings**.
3. Find `ExamplePresenter.prefab` in the sample folder.
4. Check the **Addressable** checkbox in the Inspector.
5. Set the address to `ExamplePresenter` (must match the config).
6. For testing: Set **Play Mode Script** to **Use Asset Database (fastest)**.

## Implementation Details

### Loaders
Each strategy uses a different `IUiAssetLoader` implementation:
- `PrefabRegistryUiAssetLoader`: Uses direct prefab references.
- `ResourcesUiAssetLoader`: Uses `Resources.Load`.
- `AddressablesUiAssetLoader`: Uses `Addressables.InstantiateAsync`.

### Config Assets
Each strategy has its own config asset type:
- `PrefabRegistryUiConfigs`: Stores prefab references.
- `ResourcesUiConfigs`: Stores resource paths.
- `AddressablesUiConfigs`: Stores addressable addresses.
