using System;
using System.Collections.Generic;
using GameLovers.UiService;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

// ReSharper disable once CheckNamespace

namespace GameLoversEditor.UiService
{
	/// <summary>
	/// Implementation of <see cref="UiConfigsEditorBase{TSet}"/> that syncs with a <see cref="PrefabRegistryConfig"/>.
	/// </summary>
	public abstract class PrefabRegistryUiConfigsEditor<TSet> : UiConfigsEditorBase<TSet>
		where TSet : Enum
	{
		private Dictionary<string, string> _assetPathLookup;
		private List<string> _uiConfigsAddress;
		private Dictionary<string, Type> _uiTypesByAddress;
		private SerializedProperty _prefabEntriesProperty;

		protected override void OnEnable()
		{
			base.OnEnable();
			_prefabEntriesProperty = serializedObject.FindProperty("_prefabEntries");
		}

		protected override void SyncConfigs()
		{
			var configs = new List<UiConfig>();
			_uiConfigsAddress = new List<string>();
			_assetPathLookup = new Dictionary<string, string>();
			var existingConfigs = ScriptableObjectInstance.Configs;
			var prefabConfigs = ScriptableObjectInstance as PrefabRegistryUiConfigs;

			if (prefabConfigs == null) return;

			foreach (var entry in prefabConfigs.PrefabEntries)
			{
				if (entry.Prefab == null) continue;

				var uiPresenter = entry.Prefab.GetComponent<UiPresenter>();
				if (uiPresenter == null) continue;

				var sortingOrder = GetSortingOrder(uiPresenter);
				var existingConfigIndex = existingConfigs.FindIndex(c => c.Address == entry.Address);
				var presenterType = uiPresenter.GetType();

				var config = new UiConfig
				{
					Address = entry.Address,
					Layer = existingConfigIndex >= 0 && sortingOrder < 0 ? existingConfigs[existingConfigIndex].Layer : 
					        sortingOrder < 0 ? 0 : sortingOrder,
					UiType = presenterType,
					LoadSynchronously = Attribute.IsDefined(presenterType, typeof(LoadSynchronouslyAttribute))
				};

				configs.Add(config);
				_uiConfigsAddress.Add(entry.Address);
				_assetPathLookup[entry.Address] = AssetDatabase.GetAssetPath(entry.Prefab);
			}

			ScriptableObjectInstance.Configs = configs;
			
			_uiTypesByAddress = new Dictionary<string, Type>();
			foreach (var config in configs)
			{
				if (!string.IsNullOrEmpty(config.Address) && config.UiType != null)
				{
					_uiTypesByAddress[config.Address] = config.UiType;
				}
			}

			EditorUtility.SetDirty(ScriptableObjectInstance);
		}

		public override VisualElement CreateInspectorGUI()
		{
			var root = base.CreateInspectorGUI();

			// Add Prefab Entries section at the top of Section 2 (before the configs list)
			var prefabSection = new VisualElement { style = { marginBottom = 10 } };
			var helpBox = new HelpBox(
				"Map addresses to UI Prefabs directly here. Addresses are auto-populated from prefab names. " +
				"These entries are used to generate the UI Presenter Configs below.", 
				HelpBoxMessageType.Info);
			helpBox.style.marginBottom = 5;
			prefabSection.Add(helpBox);

			var listView = new ListView
			{
				showBorder = true,
				showAddRemoveFooter = true,
				reorderable = true,
				headerTitle = "Embedded Prefab Registry",
				showFoldoutHeader = true,
				virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
				fixedItemHeight = 24
			};

			listView.BindProperty(_prefabEntriesProperty);
			listView.makeItem = CreateEntryElement;
			listView.bindItem = (element, index) => BindEntryElement(element, index, _prefabEntriesProperty);
			
			listView.itemsAdded += _ => SyncConfigs();
			listView.itemsRemoved += _ => SyncConfigs();
			listView.itemIndexChanged += (_, _) => SyncConfigs();

			// Insert after the visualizer section (index 1 in root)
			root.Insert(1, prefabSection);
			root.Insert(2, listView);

			return root;
		}

		private VisualElement CreateEntryElement()
		{
			var container = new VisualElement { style = { flexDirection = FlexDirection.Row, paddingBottom = 2, paddingTop = 2 } };
			
			var addressField = new TextField { name = "address-field", style = { flexGrow = 1, marginRight = 5 } };
			container.Add(addressField);
			
			var prefabField = new ObjectField { name = "prefab-field", objectType = typeof(GameObject), style = { flexGrow = 1 } };
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

			prefabField.RegisterValueChangedCallback(evt =>
			{
				if (evt.newValue != null && string.IsNullOrEmpty(addressProperty.stringValue))
				{
					addressProperty.stringValue = evt.newValue.name;
					addressProperty.serializedObject.ApplyModifiedProperties();
					SyncConfigs();
				}
				else
				{
					SyncConfigs();
				}
			});
			
			addressField.RegisterValueChangedCallback(_ => SyncConfigs());
		}

		protected override IReadOnlyList<string> GetAddressList() => _uiConfigsAddress ?? new List<string>();

		protected override Dictionary<string, string> GetAssetPathLookup() => _assetPathLookup;

		protected override Dictionary<string, Type> GetUiTypesByAddress() => _uiTypesByAddress;

		protected override VisualElement CreateConfigElement()
		{
			var container = new VisualElement();
			container.style.flexDirection = FlexDirection.Row;
			container.style.alignItems = Align.Center;

			container.Add(new Label { style = { flexGrow = 1, paddingLeft = 5, unityTextAlign = TextAnchor.MiddleLeft } });
			container.Add(new IntegerField { style = { width = 80, marginRight = 5 } });

			return container;
		}

		protected override void BindConfigElement(VisualElement element, int index)
		{
			if (index >= ConfigsProperty.arraySize) return;

			var itemProperty = ConfigsProperty.GetArrayElementAtIndex(index);
			var addressProperty = itemProperty.FindPropertyRelative(nameof(UiConfigs.UiConfigSerializable.Address));
			var layerProperty = itemProperty.FindPropertyRelative(nameof(UiConfigs.UiConfigSerializable.Layer));

			var label = element.Q<Label>();
			var layerField = element.Q<IntegerField>();

			label.text = addressProperty.stringValue;
			layerField.Unbind();
			layerField.BindProperty(layerProperty);
			layerField.userData = addressProperty.stringValue;
			layerField.RegisterValueChangedCallback(OnLayerChanged);
		}

		private int GetSortingOrder(UiPresenter presenter)
		{
			if (presenter.TryGetComponent<Canvas>(out var canvas)) return canvas.sortingOrder;
			if (presenter.TryGetComponent<UnityEngine.UIElements.UIDocument>(out var document)) return (int)document.sortingOrder;
			return -1;
		}
	}
}

