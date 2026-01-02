using GameLovers.UiService;
using GameLovers.UiService.Examples;
using GameLoversEditor.UiService;
using UnityEditor;

namespace GameLoversEditor.UiService.Examples
{
	/// <summary>
	/// Custom editor for the UiSets sample that uses the sample's <see cref="UiSetId"/> enum
	/// for displaying and editing UI sets in the inspector.
	/// 
	/// This demonstrates how to create a project-specific UiConfigs editor with custom set IDs.
	/// To use in your own project:
	/// 1. Define your own enum with your UI set identifiers
	/// 2. Create a CustomEditor class that inherits from the appropriate base editor
	/// 3. Pass your enum type as the generic parameter
	/// </summary>
	[CustomEditor(typeof(UiSetsSampleConfigs))]
	public class UiSetsConfigsEditor : PrefabRegistryUiConfigsEditor<UiSetId>
	{
		// No additional implementation needed - the base class handles everything
		// using the UiSetId enum for set names in the inspector
	}
}

