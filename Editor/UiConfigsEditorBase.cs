using System;
using System.Collections.Generic;
using System.Linq;
using GameLovers.UiService;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

// ReSharper disable once CheckNamespace

namespace GameLoversEditor.UiService
{
	/// <summary>
	/// Base class for <see cref="UiConfigs"/> editors with shared logic for layer visualization and set management.
	/// </summary>
	/// <typeparam name="TSet">The enum type of the <see cref="UiSetConfig"/> id</typeparam>
	public abstract class UiConfigsEditorBase<TSet> : Editor
		where TSet : Enum
	{
		protected const string VisualizerPrefsKey = "UiConfigsEditor_ShowVisualizer";

		protected UiConfigs ScriptableObjectInstance;
		protected SerializedProperty ConfigsProperty;
		protected SerializedProperty SetsProperty;
		protected bool ShowVisualizer;
		protected VisualElement VisualizerContainer;
		protected string VisualizerSearchFilter = "";

		protected virtual string ConfigExplanation => 
			"UI Presenter Configurations\n\n" +
			"Lists all UI Presenter prefabs in the game with their sorting layer values. " +
			"The Layer field controls the rendering order - higher values appear closer to the camera. " +
			"For presenters with Canvas or UIDocument components, this value directly maps to the UI sorting order.";

		protected virtual string SetExplanation =>
			"UI Set Configurations\n\n" +
			"UI Sets group multiple presenter instances that should be displayed together. " +
			"When a set is activated via UiService, all its presenters are loaded and shown simultaneously. " +
			"Presenters are loaded in the order listed (top to bottom).";

		protected virtual void OnEnable()
		{
			ScriptableObjectInstance = target as UiConfigs;
			if (ScriptableObjectInstance == null) return;

			SyncConfigs();
			
			// Ensure sets array matches enum size
			ScriptableObjectInstance.SetSetsSize(Enum.GetNames(typeof(TSet)).Length);
			
			serializedObject.Update();
			
			ConfigsProperty = serializedObject.FindProperty("_configs");
			SetsProperty = serializedObject.FindProperty("_sets");
			
			ShowVisualizer = EditorPrefs.GetBool(VisualizerPrefsKey, false);
		}

		/// <inheritdoc />
		public override VisualElement CreateInspectorGUI()
		{
			var root = new VisualElement();
			root.style.paddingTop = 5;
			root.style.paddingBottom = 5;
			root.style.paddingLeft = 3;
			root.style.paddingRight = 3;

			// Section 0: Layer Visualizer (collapsible)
			var visualizerSection = CreateVisualizerSection();
			root.Add(visualizerSection);

			// Section 1: UI Config Explanation
			var configHelpBox = new HelpBox(ConfigExplanation, HelpBoxMessageType.Info);
			configHelpBox.style.marginBottom = 10;
			root.Add(configHelpBox);

			// Section 2: UI Configs List
			var configsListView = CreateConfigsListView();
			root.Add(configsListView);

			// Section 3: Separator
			var separator = new VisualElement();
			separator.style.height = 1;
			separator.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
			separator.style.marginTop = 15;
			separator.style.marginBottom = 15;
			root.Add(separator);

			// Section 4: UI Set Explanation
			var setHelpBox = new HelpBox(SetExplanation, HelpBoxMessageType.Info);
			setHelpBox.style.marginBottom = 10;
			root.Add(setHelpBox);

			// Section 5: UI Sets List
			var setsContainer = CreateSetsContainer();
			root.Add(setsContainer);

			return root;
		}

		protected abstract void SyncConfigs();
		protected abstract IReadOnlyList<string> GetAddressList();
		protected abstract Dictionary<string, string> GetAssetPathLookup();
		protected abstract Dictionary<string, Type> GetUiTypesByAddress();

		protected virtual ListView CreateConfigsListView()
		{
			var listView = new ListView
			{
				showBorder = true,
				showFoldoutHeader = true,
				headerTitle = "UI Presenter Configs",
				showAddRemoveFooter = false,
				showBoundCollectionSize = false,
				reorderable = false,
				virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
				fixedItemHeight = 22
			};

			listView.style.minHeight = 20;
			listView.style.marginBottom = 5;

			listView.BindProperty(ConfigsProperty);

			listView.makeItem = CreateConfigElement;
			listView.bindItem = BindConfigElement;

			return listView;
		}

		protected abstract VisualElement CreateConfigElement();
		protected abstract void BindConfigElement(VisualElement element, int index);

		protected virtual void OnLayerChanged(ChangeEvent<int> evt)
		{
			if (evt.newValue == evt.previousValue) return;

			var layerField = evt.target as IntegerField;
			if (layerField?.userData is string address)
			{
				SyncLayerToPrefab(address, evt.newValue);
			}
		}

		protected virtual void SyncLayerToPrefab(string address, int newLayer)
		{
			var pathLookup = GetAssetPathLookup();
			if (pathLookup == null || !pathLookup.TryGetValue(address, out var assetPath)) return;

			var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
			if (prefab == null) return;

			bool changed = false;

			if (prefab.TryGetComponent<Canvas>(out var canvas))
			{
				canvas.sortingOrder = newLayer;
				changed = true;
			}
			else if (prefab.TryGetComponent<UnityEngine.UIElements.UIDocument>(out var document))
			{
				document.sortingOrder = newLayer;
				changed = true;
			}

			if (changed)
			{
				EditorUtility.SetDirty(prefab);
				AssetDatabase.SaveAssets();
			}
		}

		private VisualElement CreateSetsContainer()
		{
			var container = new VisualElement();
			var enumNames = Enum.GetNames(typeof(TSet));

			for (int setIndex = 0; setIndex < enumNames.Length; setIndex++)
			{
				var setElement = CreateSetElement(enumNames[setIndex], setIndex);
				container.Add(setElement);
			}

			return container;
		}

		private VisualElement CreateSetElement(string setName, int setIndex)
		{
			var setContainer = new VisualElement();
			setContainer.style.paddingLeft = 5;
			setContainer.style.paddingRight = 5;
			setContainer.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.2f);
			setContainer.style.borderBottomLeftRadius = 4;
			setContainer.style.borderBottomRightRadius = 4;
			setContainer.style.borderTopLeftRadius = 4;
			setContainer.style.borderTopRightRadius = 4;
			setContainer.style.marginBottom = 5;

			// Header
			var header = new Label($"{setName} Set");
			header.style.unityFontStyleAndWeight = FontStyle.Bold;
			header.style.fontSize = 13;
			header.style.marginBottom = 5;
			setContainer.Add(header);

			// Get the property for this set's UI entries
			var setProperty = SetsProperty.GetArrayElementAtIndex(setIndex);
			var uiEntriesProperty = setProperty.FindPropertyRelative(nameof(UiSetConfigSerializable.UiEntries));

			// ListView for presenters in this set
			var presenterListView = new ListView
			{
				showBorder = true,
				showAddRemoveFooter = true,
				reorderable = true,
				showBoundCollectionSize = false,
				virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
				fixedItemHeight = 28
			};

			presenterListView.BindProperty(uiEntriesProperty);

			presenterListView.makeItem = CreateSetPresenterElement;
			presenterListView.bindItem = (element, index) => BindSetPresenterElement(element, index, uiEntriesProperty, presenterListView);
			
			presenterListView.itemsAdded += indices => OnPresenterItemsAdded(indices, uiEntriesProperty);
			presenterListView.itemsRemoved += _ => SaveSetChanges();
			presenterListView.itemIndexChanged += (_, _) => SaveSetChanges();

			setContainer.Add(presenterListView);

			return setContainer;
		}

		private VisualElement CreateSetPresenterElement()
		{
			var container = new VisualElement();
			container.style.flexDirection = FlexDirection.Row;
			container.style.alignItems = Align.Center;
			
			// Drag handle
			var dragHandle = new Label("☰");
			dragHandle.style.width = 20;
			dragHandle.style.unityTextAlign = TextAnchor.MiddleCenter;
			dragHandle.style.marginLeft = 3;
			dragHandle.style.marginRight = 5;
			dragHandle.style.fontSize = 16;
			dragHandle.style.color = new Color(0.7f, 0.7f, 0.7f);
			container.Add(dragHandle);
			
			// Dropdown
			var dropdown = new DropdownField();
			dropdown.style.flexGrow = 1;
			dropdown.style.paddingTop = 3;
			dropdown.style.paddingBottom = 3;
			dropdown.name = "ui-type-dropdown";
			container.Add(dropdown);
			
			// Delete button
			var deleteButton = new Button { text = "×" };
			deleteButton.style.width = 25;
			deleteButton.style.height = 20;
			deleteButton.style.marginLeft = 5;
			deleteButton.name = "delete-button";
			container.Add(deleteButton);
			
			return container;
		}

		private void BindSetPresenterElement(VisualElement element, int index, SerializedProperty uiEntriesProperty, ListView listView)
		{
			if (index >= uiEntriesProperty.arraySize) return;

			var dropdown = element.Q<DropdownField>("ui-type-dropdown");
			if (dropdown == null) return;

			var entryProperty = uiEntriesProperty.GetArrayElementAtIndex(index);
			var typeNameProperty = entryProperty.FindPropertyRelative(nameof(UiSetEntry.UiTypeName));
			var instanceAddressProperty = entryProperty.FindPropertyRelative(nameof(UiSetEntry.InstanceAddress));
			
			var addressList = GetAddressList();
			var uiTypesByAddress = GetUiTypesByAddress();
			dropdown.choices = new List<string>(addressList);

			// Find matching address for type
			var currentTypeName = typeNameProperty.stringValue;
			Type currentType = string.IsNullOrEmpty(currentTypeName) ? null : Type.GetType(currentTypeName);
			
			string matchingAddress = null;
			if (currentType != null && uiTypesByAddress != null)
			{
				foreach (var kvp in uiTypesByAddress)
				{
					if (kvp.Value == currentType)
					{
						matchingAddress = kvp.Key;
						break;
					}
				}
			}
			
			var selectedIndex = string.IsNullOrEmpty(matchingAddress) ? 0 : addressList.ToList().FindIndex(a => a == matchingAddress);
			if (selectedIndex < 0) selectedIndex = 0;

			if (addressList.Count > 0)
			{
				dropdown.Unbind();
				dropdown.index = selectedIndex;
				
				dropdown.RegisterValueChangedCallback(_ =>
				{
					var newIndex = dropdown.index;
					if (newIndex >= 0 && newIndex < addressList.Count)
					{
						var selectedAddress = addressList[newIndex];
						if (uiTypesByAddress.TryGetValue(selectedAddress, out var selectedType))
						{
							typeNameProperty.stringValue = selectedType.AssemblyQualifiedName;
							instanceAddressProperty.stringValue = selectedAddress;
							SaveSetChanges();
						}
					}
				});
				
				if (string.IsNullOrEmpty(typeNameProperty.stringValue) && selectedIndex < addressList.Count)
				{
					var address = addressList[selectedIndex];
					if (uiTypesByAddress.TryGetValue(address, out var type))
					{
						typeNameProperty.stringValue = type.AssemblyQualifiedName;
						instanceAddressProperty.stringValue = address;
						serializedObject.ApplyModifiedProperties();
					}
				}
			}

			var deleteButton = element.Q<Button>("delete-button");
			if (deleteButton != null)
			{
				if (deleteButton.userData is EventCallback<ClickEvent> previousCallback)
				{
					deleteButton.UnregisterCallback(previousCallback);
				}

				EventCallback<ClickEvent> clickHandler = _ =>
				{
					uiEntriesProperty.DeleteArrayElementAtIndex(index);
					SaveSetChanges();
				};

				deleteButton.userData = clickHandler;
				deleteButton.RegisterCallback(clickHandler);
			}
		}

		private void OnPresenterItemsAdded(IEnumerable<int> indices, SerializedProperty uiEntriesProperty)
		{
			var addressList = GetAddressList();
			var uiTypesByAddress = GetUiTypesByAddress();
			if (addressList.Count == 0 || uiTypesByAddress == null) return;

			var defaultAddress = addressList[0];
			Type defaultType = uiTypesByAddress.TryGetValue(defaultAddress, out var type) ? type : null;
			
			foreach (var index in indices)
			{
				if (index < uiEntriesProperty.arraySize)
				{
					var entryProperty = uiEntriesProperty.GetArrayElementAtIndex(index);
					entryProperty.FindPropertyRelative(nameof(UiSetEntry.UiTypeName)).stringValue = defaultType?.AssemblyQualifiedName ?? string.Empty;
					entryProperty.FindPropertyRelative(nameof(UiSetEntry.InstanceAddress)).stringValue = defaultAddress;
				}
			}
		
			SaveSetChanges();
		}

		protected void SaveSetChanges()
		{
			serializedObject.ApplyModifiedProperties();
			if (ScriptableObjectInstance != null)
			{
				EditorUtility.SetDirty(ScriptableObjectInstance);
				AssetDatabase.SaveAssets();
			}
		}

		protected VisualElement CreateVisualizerSection()
		{
			var section = new VisualElement();
			section.style.marginBottom = 15;
			
			var header = new VisualElement();
			header.style.flexDirection = FlexDirection.Row;
			header.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f);
			header.style.paddingTop = 5;
			header.style.paddingBottom = 5;
			header.style.paddingLeft = 5;
			header.style.paddingRight = 5;
			header.style.borderTopLeftRadius = 4;
			header.style.borderTopRightRadius = 4;
			
			var toggleButton = new Button(() => ToggleVisualizer())
			{
				text = ShowVisualizer ? "▼" : "▶"
			};
			toggleButton.style.width = 25;
			toggleButton.style.marginRight = 5;
			
			var titleLabel = new Label("UI Layer Hierarchy Visualizer");
			titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			titleLabel.style.flexGrow = 1;
			
			header.Add(toggleButton);
			header.Add(titleLabel);
			section.Add(header);
			
			VisualizerContainer = new VisualElement();
			VisualizerContainer.style.backgroundColor = new Color(0.15f, 0.15f, 0.15f);
			VisualizerContainer.style.paddingTop = 10;
			VisualizerContainer.style.paddingBottom = 10;
			VisualizerContainer.style.paddingLeft = 5;
			VisualizerContainer.style.paddingRight = 5;
			VisualizerContainer.style.borderBottomLeftRadius = 4;
			VisualizerContainer.style.borderBottomRightRadius = 4;
			VisualizerContainer.style.display = ShowVisualizer ? DisplayStyle.Flex : DisplayStyle.None;
			
			var toolbar = new VisualElement();
			toolbar.style.flexDirection = FlexDirection.Row;
			toolbar.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
			toolbar.style.paddingTop = 3;
			toolbar.style.paddingBottom = 3;
			toolbar.style.paddingLeft = 5;
			toolbar.style.paddingRight = 5;
			toolbar.style.marginBottom = 10;
			toolbar.style.borderTopLeftRadius = 3;
			toolbar.style.borderTopRightRadius = 3;
			toolbar.style.borderBottomLeftRadius = 3;
			toolbar.style.borderBottomRightRadius = 3;
			
			var spacer = new VisualElement { style = { flexGrow = 1 } };
			toolbar.Add(spacer);
			
			var searchLabel = new Label("Search:") { style = { marginRight = 5, unityTextAlign = TextAnchor.MiddleCenter } };
			toolbar.Add(searchLabel);
			
			var searchField = new ToolbarSearchField { style = { width = 200 }, value = VisualizerSearchFilter };
			searchField.RegisterValueChangedCallback(evt =>
			{
				VisualizerSearchFilter = evt.newValue;
				RefreshVisualizerContent();
			});
			toolbar.Add(searchField);
			VisualizerContainer.Add(toolbar);
			
			var scrollView = new ScrollView { style = { maxHeight = 500, marginTop = 5 } };
			BuildVisualizerLayerHierarchy(scrollView);
			VisualizerContainer.Add(scrollView);
			
			section.Add(VisualizerContainer);
			
			return section;
		}

		protected void ToggleVisualizer()
		{
			ShowVisualizer = !ShowVisualizer;
			EditorPrefs.SetBool(VisualizerPrefsKey, ShowVisualizer);
			
			if (VisualizerContainer != null)
			{
				VisualizerContainer.style.display = ShowVisualizer ? DisplayStyle.Flex : DisplayStyle.None;
				var toggleButton = VisualizerContainer.parent.Q<Button>();
				if (toggleButton != null) toggleButton.text = ShowVisualizer ? "▼" : "▶";
				if (ShowVisualizer) RefreshVisualizerContent();
			}
		}

		protected void RefreshVisualizerContent()
		{
			if (VisualizerContainer == null) return;
			var scrollView = VisualizerContainer.Q<ScrollView>();
			if (scrollView != null)
			{
				scrollView.Clear();
				BuildVisualizerLayerHierarchy(scrollView);
			}
		}

		protected virtual void BuildVisualizerLayerHierarchy(ScrollView scrollView)
		{
			var configs = ScriptableObjectInstance?.Configs;
			if (configs == null || configs.Count == 0)
			{
				scrollView.Add(new HelpBox("No UI configurations found", HelpBoxMessageType.Info));
				return;
			}
			
			var filteredConfigs = string.IsNullOrEmpty(VisualizerSearchFilter) ? configs : 
				configs.Where(c => c.UiType.Name.ToLower().Contains(VisualizerSearchFilter.ToLower()) || 
				                   c.Address.ToLower().Contains(VisualizerSearchFilter.ToLower())).ToList();
			
			var layerGroups = filteredConfigs.GroupBy(c => c.Layer).OrderBy(g => g.Key).ToList();
			var layersContainer = new VisualElement { style = { marginLeft = 5, marginRight = 5 } };
			
			foreach (var layerGroup in layerGroups)
			{
				layersContainer.Add(CreateVisualizerLayerElement(layerGroup.Key, layerGroup.ToList()));
			}
			
			scrollView.Add(layersContainer);
			scrollView.Add(CreateVisualizerStatistics(filteredConfigs));
		}

		private VisualElement CreateVisualizerLayerElement(int layer, List<UiConfig> configs)
		{
			var container = new VisualElement();
			container.style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);
			container.style.borderTopLeftRadius = 4;
			container.style.borderTopRightRadius = 4;
			container.style.borderBottomLeftRadius = 4;
			container.style.borderBottomRightRadius = 4;
			container.style.marginBottom = 5;
			container.style.paddingTop = 5;
			container.style.paddingBottom = 5;
			
			var header = new VisualElement();
			header.style.flexDirection = FlexDirection.Row;
			header.style.backgroundColor = GetVisualizerLayerColor(layer);
			header.style.paddingTop = 5;
			header.style.paddingBottom = 5;
			header.style.paddingLeft = 10;
			header.style.paddingRight = 10;
			header.style.marginBottom = 5;
			header.style.alignItems = Align.Center;
			
			var textContainer = new VisualElement();
			textContainer.style.flexDirection = FlexDirection.Row;
			textContainer.style.backgroundColor = new Color(0, 0, 0, 0.5f);
			textContainer.style.paddingLeft = 8;
			textContainer.style.paddingRight = 8;
			textContainer.style.paddingTop = 3;
			textContainer.style.paddingBottom = 3;
			textContainer.style.borderTopLeftRadius = 3;
			textContainer.style.borderTopRightRadius = 3;
			textContainer.style.borderBottomLeftRadius = 3;
			textContainer.style.borderBottomRightRadius = 3;
			
			var layerLabel = new Label($"Layer {layer}");
			layerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
			layerLabel.style.fontSize = 13;
			layerLabel.style.color = Color.white;
			textContainer.Add(layerLabel);
			
			var countLabel = new Label($"({configs.Count} UI{(configs.Count > 1 ? "s" : "")})");
			countLabel.style.fontSize = 11;
			countLabel.style.color = Color.white;
			countLabel.style.marginLeft = 5;
			textContainer.Add(countLabel);
			
			header.Add(textContainer);
			container.Add(header);
			
			foreach (var config in configs)
			{
				var item = new VisualElement();
				item.style.flexDirection = FlexDirection.Row;
				item.style.paddingLeft = 20;
				item.style.paddingRight = 10;
				item.style.paddingTop = 2;
				item.style.paddingBottom = 2;
				
				item.Add(new Label(config.UiType.Name) { style = { width = 200 } });
				item.Add(new Label(config.Address) { style = { fontSize = 10, color = new Color(0.7f, 0.7f, 0.7f), flexGrow = 1 } });
				
				if (config.LoadSynchronously)
				{
					item.Add(new Label("[SYNC]") { style = { unityFontStyleAndWeight = FontStyle.Bold, width = 50, color = new Color(1, 0.8f, 0) } });
				}
				container.Add(item);
			}
			
			return container;
		}

		private VisualElement CreateVisualizerStatistics(List<UiConfig> configs)
		{
			var container = new VisualElement { style = { marginTop = 10, marginLeft = 5, marginBottom = 10 } };
			container.Add(new Label("Statistics") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 5 } });
			
			var stats = new VisualElement { style = { marginLeft = 15 } };
			stats.Add(new Label($"Total UIs: {configs.Count}"));
			stats.Add(new Label($"Layers Used: {configs.Select(c => c.Layer).Distinct().Count()}"));
			stats.Add(new Label($"Synchronous Loads: {configs.Count(c => c.LoadSynchronously)}"));
			stats.Add(new Label($"Async Loads: {configs.Count(c => !c.LoadSynchronously)}"));
			
			container.Add(stats);
			return container;
		}

		private Color GetVisualizerLayerColor(int layer)
		{
			if (layer < 0) return new Color(0.8f, 0.2f, 0.2f);
			if (layer == 0) return new Color(0.3f, 0.3f, 0.3f);
			var hue = (layer * 0.1f) % 1f;
			return Color.HSVToRGB(hue, 0.5f, 0.9f);
		}
	}
}

