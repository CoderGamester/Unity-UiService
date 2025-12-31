using GameLovers.UiService;
using UnityEditor;

namespace GameLoversEditor.UiService
{
	/// <summary>
	/// Default UI Set identifiers for out-of-the-box usage.
	/// Users can create their own enum and custom editor to override these defaults.
	/// </summary>
	public enum DefaultUiSetId
	{
		InitialLoading = 0,
		MainMenu = 1,
		Gameplay = 2,
		Settings = 3,
		Overlays = 4,
		Popups = 5
	}

	/// <summary>
	/// Default implementation of the UiConfigs editor for Addressables-based loading.
	/// This allows the library to work out-of-the-box without requiring user implementation.
	/// Users can override by creating their own CustomEditor implementation for <see cref="AddressablesUiConfigs"/>.
	/// </summary>
	[CustomEditor(typeof(AddressablesUiConfigs))]
	public class DefaultAddressablesUiConfigsEditor : AddressablesUiConfigsEditor<DefaultUiSetId>
	{
		// No additional implementation needed - uses Addressables loader functionality by default
	}

	/// <summary>
	/// Default implementation of the UiConfigs editor for Resources folder-based loading.
	/// This allows the library to work out-of-the-box without requiring user implementation.
	/// Users can override by creating their own CustomEditor implementation for <see cref="ResourcesUiConfigs"/>.
	/// </summary>
	[CustomEditor(typeof(ResourcesUiConfigs))]
	public class DefaultResourcesUiConfigsEditor : ResourcesUiConfigsEditor<DefaultUiSetId>
	{
		// No additional implementation needed - uses Resources loader functionality by default
	}

	/// <summary>
	/// Default implementation of the UiConfigs editor for PrefabRegistry-based loading.
	/// This allows the library to work out-of-the-box without requiring user implementation.
	/// Users can override by creating their own CustomEditor implementation for <see cref="PrefabRegistryUiConfigs"/>.
	/// </summary>
	[CustomEditor(typeof(PrefabRegistryUiConfigs))]
	public class DefaultPrefabRegistryUiConfigsEditor : PrefabRegistryUiConfigsEditor<DefaultUiSetId>
	{
		// No additional implementation needed - uses PrefabRegistry loader functionality by default
	}

	/// <summary>
	/// Fallback editor for the base <see cref="UiConfigs"/> type.
	/// This handles legacy assets created before the specific types were introduced.
	/// </summary>
	[CustomEditor(typeof(UiConfigs))]
	public class DefaultUiConfigsEditor : AddressablesUiConfigsEditor<DefaultUiSetId>
	{
		// Falls back to Addressables behavior for backward compatibility
	}
}
