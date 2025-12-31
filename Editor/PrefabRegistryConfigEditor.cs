using System.Collections.Generic;
using GameLovers.UiService;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

// ReSharper disable once CheckNamespace

namespace GameLoversEditor.UiService
{
	/// <summary>
	/// Custom editor for <see cref="PrefabRegistryConfig"/> using UI Toolkit.
	/// </summary>
	[CustomEditor(typeof(PrefabRegistryConfig))]
	public class PrefabRegistryConfigEditor : Editor
	{
		public override VisualElement CreateInspectorGUI()
		{
			var root = new VisualElement { style = { paddingLeft = 5, paddingRight = 5, paddingTop = 5 } };

			var helpBox = new HelpBox(
				"Map addresses (string keys) to UI Prefabs. These entries are used by PrefabRegistryUiAssetLoader and can be synced with UiConfigs.", 
				HelpBoxMessageType.Info);
			helpBox.style.marginBottom = 10;
			root.Add(helpBox);

			var entriesProperty = serializedObject.FindProperty("_entries");
			
			var listView = new ListView
			{
				showBorder = true,
				showAddRemoveFooter = true,
				reorderable = true,
				headerTitle = "Prefab Registry Entries",
				showFoldoutHeader = true,
				virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
				fixedItemHeight = 24
			};

			listView.BindProperty(entriesProperty);
			listView.makeItem = CreateEntryElement;
			listView.bindItem = (element, index) => BindEntryElement(element, index, entriesProperty);

			root.Add(listView);
			
			return root;
		}

		private VisualElement CreateEntryElement()
		{
			var container = new VisualElement { style = { flexDirection = FlexDirection.Row, paddingBottom = 2, paddingTop = 2 } };
			
			var addressField = new TextField { name = "address-field", style = { flexGrow = 1, marginRight = 5 } };
			addressField.tooltip = "Address key used in UiService calls";
			container.Add(addressField);
			
			var prefabField = new ObjectField { name = "prefab-field", objectType = typeof(GameObject), style = { flexGrow = 1 } };
			prefabField.tooltip = "UI Presenter Prefab";
			container.Add(prefabField);
			
			return container;
		}

		private void BindEntryElement(VisualElement element, int index, SerializedProperty entriesProperty)
		{
			if (index >= entriesProperty.arraySize) return;

			var itemProperty = entriesProperty.GetArrayElementAtIndex(index);
			var addressProperty = itemProperty.FindPropertyRelative("Address");
			var prefabProperty = itemProperty.FindPropertyRelative("Prefab");

			var addressField = element.Q<TextField>("address-field");
			var prefabField = element.Q<ObjectField>("prefab-field");

			addressField.BindProperty(addressProperty);
			prefabField.BindProperty(prefabProperty);
		}
	}
}

