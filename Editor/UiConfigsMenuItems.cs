using GameLovers.UiService;
using UnityEditor;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace GameLoversEditor.UiService
{
	/// <summary>
	/// Helps selecting the <see cref="UiConfigs"/> asset file in the Editor
	/// </summary>
	public static class UiConfigsMenuItems
	{
		[MenuItem("Tools/UI Service/Select UiConfigs")]
		private static void SelectUiConfigs()
		{
			// Find any UiConfigs asset (including derived types like AddressablesUiConfigs, ResourcesUiConfigs, etc.)
			var assets = AssetDatabase.FindAssets($"t:{nameof(UiConfigs)}");
			var scriptableObject = assets.Length > 0 ? 
				AssetDatabase.LoadAssetAtPath<UiConfigs>(AssetDatabase.GUIDToAssetPath(assets[0])) :
				ScriptableObject.CreateInstance<AddressablesUiConfigs>();

			if (assets.Length == 0)
			{
				// Create AddressablesUiConfigs by default for backward compatibility
				AssetDatabase.CreateAsset(scriptableObject, $"Assets/{nameof(AddressablesUiConfigs)}.asset");
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}

			Selection.activeObject = scriptableObject;
			FocusInspectorWindow();
		}

		[MenuItem("Tools/UI Service/Layer Visualizer")]
		public static void ShowLayerVisualizer()
		{
			// Set the pref BEFORE selecting so OnEnable reads the correct value
			EditorPrefs.SetBool("UiConfigsEditor_ShowVisualizer", true);
			
			SelectUiConfigs();

			// Force inspector refresh to rebuild the UI
			ActiveEditorTracker.sharedTracker.ForceRebuild();
		}
		
		private static void FocusInspectorWindow()
		{
			// Get the Inspector window type using reflection
			var inspectorType = typeof(Editor).Assembly.GetType("UnityEditor.InspectorWindow");
			if (inspectorType != null)
			{
				// Focus or create the Inspector window
				EditorWindow.GetWindow(inspectorType);
			}
		}
	}
}
