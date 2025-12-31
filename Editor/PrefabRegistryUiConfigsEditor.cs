using System;
using System.Collections.Generic;
using System.Linq;
using GameLovers.UiService;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

// ReSharper disable once CheckNamespace

namespace GameLoversEditor.UiService
{
	/// <summary>
	/// Implementation of <see cref="UiConfigsEditorBase{TSet}"/> that syncs with a <see cref="PrefabRegistryUiConfigs"/>.
	/// </summary>
	public abstract class PrefabRegistryUiConfigsEditor<TSet> : UiConfigsEditorBase<TSet>
		where TSet : Enum
	{
		private Dictionary<string, string> _assetPathLookup;
		private List<string> _uiConfigsAddress;
		private Dictionary<string, Type> _uiTypesByAddress;
		private SerializedProperty _prefabEntriesProperty;
		private ListView _prefabListView;

		protected override void OnEnable()
		{
			// Must initialize _prefabEntriesProperty before base.OnEnable() 
			// because SyncConfigs() is called there and needs this property
			_prefabEntriesProperty = serializedObject.FindProperty("_prefabEntries");
			base.OnEnable();
		}

		protected override void SyncConfigs()
		{
			var configs = new List<UiConfig>();
			_uiConfigsAddress = new List<string>();
			_assetPathLookup = new Dictionary<string, string>();
			var existingConfigs = ScriptableObjectInstance.Configs;
			var prefabConfigs = ScriptableObjectInstance as PrefabRegistryUiConfigs;

			if (prefabConfigs == null) return;

			// First pass: update addresses from prefab names
			UpdateAddressesFromPrefabs();

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

		/// <summary>
		/// Updates addresses in prefab entries to match their prefab names.
		/// Called during SyncConfigs to ensure addresses are always derived from prefab names.
		/// </summary>
		private void UpdateAddressesFromPrefabs()
		{
			if (_prefabEntriesProperty == null) return;
			
			serializedObject.Update();
			var modified = false;

			for (int i = 0; i < _prefabEntriesProperty.arraySize; i++)
			{
				var itemProperty = _prefabEntriesProperty.GetArrayElementAtIndex(i);
				var addressProperty = itemProperty.FindPropertyRelative("Address");
				var prefabProperty = itemProperty.FindPropertyRelative("Prefab");

				var prefab = prefabProperty.objectReferenceValue as GameObject;
				if (prefab != null && addressProperty.stringValue != prefab.name)
				{
					addressProperty.stringValue = prefab.name;
					modified = true;
				}
			}

			if (modified)
			{
				serializedObject.ApplyModifiedProperties();
			}
		}

		/// <summary>
		/// Adds multiple prefabs to the registry at once.
		/// Validates that each prefab has a UiPresenter component and prevents duplicates.
		/// </summary>
		/// <param name="prefabs">The prefabs to add</param>
		private void AddPrefabs(IEnumerable<GameObject> prefabs)
		{
			serializedObject.Update();
			var existingPrefabs = new HashSet<GameObject>();

			// Collect existing prefabs to prevent duplicates
			for (int i = 0; i < _prefabEntriesProperty.arraySize; i++)
			{
				var itemProperty = _prefabEntriesProperty.GetArrayElementAtIndex(i);
				var prefabProperty = itemProperty.FindPropertyRelative("Prefab");
				if (prefabProperty.objectReferenceValue is GameObject existingPrefab)
				{
					existingPrefabs.Add(existingPrefab);
				}
			}

			var addedCount = 0;
			var skippedNoPresenter = 0;
			var skippedDuplicate = 0;

			foreach (var prefab in prefabs)
			{
				if (prefab == null) continue;

				// Skip duplicates
				if (existingPrefabs.Contains(prefab))
				{
					skippedDuplicate++;
					continue;
				}

				// Validate the prefab has a UiPresenter component
				if (prefab.GetComponent<UiPresenter>() == null)
				{
					skippedNoPresenter++;
					continue;
				}

				// Add new entry
				var newIndex = _prefabEntriesProperty.arraySize;
				_prefabEntriesProperty.InsertArrayElementAtIndex(newIndex);
				var newItem = _prefabEntriesProperty.GetArrayElementAtIndex(newIndex);
				newItem.FindPropertyRelative("Address").stringValue = prefab.name;
				newItem.FindPropertyRelative("Prefab").objectReferenceValue = prefab;
				
				existingPrefabs.Add(prefab);
				addedCount++;
			}

			serializedObject.ApplyModifiedProperties();
			SyncConfigs();
			_prefabListView?.RefreshItems();

			// Show feedback if there were skipped items
			if (skippedNoPresenter > 0 || skippedDuplicate > 0)
			{
				var message = $"Added {addedCount} prefab(s).";
				if (skippedDuplicate > 0) message += $" Skipped {skippedDuplicate} duplicate(s).";
				if (skippedNoPresenter > 0) message += $" Skipped {skippedNoPresenter} without UiPresenter.";
				Debug.Log($"[PrefabRegistryUiConfigs] {message}");
			}
		}

		public override VisualElement CreateInspectorGUI()
		{
			var root = base.CreateInspectorGUI();

			// Add Prefab Entries section at the top of Section 2 (before the configs list)
			var prefabSection = new VisualElement { style = { marginBottom = 10 } };
			var helpBox = new HelpBox(
				"Drag and drop UI Prefabs below. Addresses are automatically derived from prefab names. " +
				"Only prefabs with a UiPresenter component are accepted.", 
				HelpBoxMessageType.Info);
			helpBox.style.marginBottom = 5;
			prefabSection.Add(helpBox);

			// Create drop zone for batch prefab dropping
			var dropZone = CreateDropZone();
			prefabSection.Add(dropZone);

			_prefabListView = new ListView
			{
				showBorder = true,
				showAddRemoveFooter = true,
				reorderable = true,
				headerTitle = "Embedded Prefab Registry",
				showFoldoutHeader = true,
				virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
				fixedItemHeight = 24
			};

			_prefabListView.BindProperty(_prefabEntriesProperty);
			_prefabListView.makeItem = CreateEntryElement;
			_prefabListView.bindItem = (element, index) => BindEntryElement(element, index, _prefabEntriesProperty);
			
			_prefabListView.itemsAdded += _ => SyncConfigs();
			_prefabListView.itemsRemoved += _ => SyncConfigs();
			_prefabListView.itemIndexChanged += (_, _) => SyncConfigs();

			// Insert after the visualizer section (index 1 in root)
			root.Insert(1, prefabSection);
			root.Insert(2, _prefabListView);

			return root;
		}

		/// <summary>
		/// Creates a drop zone for batch prefab dropping.
		/// </summary>
		private VisualElement CreateDropZone()
		{
			var dropZone = new VisualElement();
			dropZone.style.height = 50;
			dropZone.style.marginBottom = 10;
			dropZone.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f, 0.5f);
			dropZone.style.borderTopLeftRadius = 4;
			dropZone.style.borderTopRightRadius = 4;
			dropZone.style.borderBottomLeftRadius = 4;
			dropZone.style.borderBottomRightRadius = 4;
			dropZone.style.borderTopWidth = 2;
			dropZone.style.borderBottomWidth = 2;
			dropZone.style.borderLeftWidth = 2;
			dropZone.style.borderRightWidth = 2;
			dropZone.style.borderTopColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
			dropZone.style.borderBottomColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
			dropZone.style.borderLeftColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
			dropZone.style.borderRightColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
			dropZone.style.justifyContent = Justify.Center;
			dropZone.style.alignItems = Align.Center;

			var dropLabel = new Label("Drop Prefabs Here (supports multiple)");
			dropLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
			dropLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
			dropZone.Add(dropLabel);

			// Register drag and drop callbacks
			dropZone.RegisterCallback<DragEnterEvent>(evt =>
			{
				if (HasValidPrefabsInDrag())
				{
					dropZone.style.backgroundColor = new Color(0.2f, 0.4f, 0.2f, 0.5f);
					dropZone.style.borderTopColor = new Color(0.3f, 0.8f, 0.3f, 0.8f);
					dropZone.style.borderBottomColor = new Color(0.3f, 0.8f, 0.3f, 0.8f);
					dropZone.style.borderLeftColor = new Color(0.3f, 0.8f, 0.3f, 0.8f);
					dropZone.style.borderRightColor = new Color(0.3f, 0.8f, 0.3f, 0.8f);
					dropLabel.text = "Release to add prefabs";
					dropLabel.style.color = new Color(0.8f, 1f, 0.8f);
				}
			});

			dropZone.RegisterCallback<DragLeaveEvent>(evt =>
			{
				ResetDropZoneStyle(dropZone, dropLabel);
			});

			dropZone.RegisterCallback<DragUpdatedEvent>(evt =>
			{
				if (HasValidPrefabsInDrag())
				{
					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
				}
			});

			dropZone.RegisterCallback<DragPerformEvent>(evt =>
			{
				var prefabs = GetPrefabsFromDrag();
				if (prefabs.Count > 0)
				{
					AddPrefabs(prefabs);
				}
				ResetDropZoneStyle(dropZone, dropLabel);
			});

			return dropZone;
		}

		private void ResetDropZoneStyle(VisualElement dropZone, Label dropLabel)
		{
			dropZone.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f, 0.5f);
			dropZone.style.borderTopColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
			dropZone.style.borderBottomColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
			dropZone.style.borderLeftColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
			dropZone.style.borderRightColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);
			dropLabel.text = "Drop Prefabs Here (supports multiple)";
			dropLabel.style.color = new Color(0.7f, 0.7f, 0.7f);
		}

		private bool HasValidPrefabsInDrag()
		{
			return DragAndDrop.objectReferences.Any(obj => obj is GameObject);
		}

		private List<GameObject> GetPrefabsFromDrag()
		{
			var prefabs = new List<GameObject>();
			foreach (var obj in DragAndDrop.objectReferences)
			{
				if (obj is GameObject go && PrefabUtility.IsPartOfPrefabAsset(go))
				{
					prefabs.Add(go);
				}
			}
			return prefabs;
		}

		private VisualElement CreateEntryElement()
		{
			var container = new VisualElement { style = { flexDirection = FlexDirection.Row, paddingBottom = 2, paddingTop = 2 } };
			
			// Read-only label showing the address (derived from prefab name)
			var addressLabel = new Label 
			{ 
				name = "address-label", 
				style = 
				{ 
					flexGrow = 1, 
					marginRight = 5,
					unityTextAlign = TextAnchor.MiddleLeft,
					color = new Color(0.6f, 0.6f, 0.6f),
					fontSize = 11,
					paddingLeft = 3
				} 
			};
			container.Add(addressLabel);
			
			var prefabField = new ObjectField { name = "prefab-field", objectType = typeof(GameObject), style = { flexGrow = 2 } };
			container.Add(prefabField);
			
			return container;
		}

		private void BindEntryElement(VisualElement element, int index, SerializedProperty entriesProperty)
		{
			if (index >= entriesProperty.arraySize) return;

			var itemProperty = entriesProperty.GetArrayElementAtIndex(index);
			var addressProperty = itemProperty.FindPropertyRelative("Address");
			var prefabProperty = itemProperty.FindPropertyRelative("Prefab");

			var addressLabel = element.Q<Label>("address-label");
			var prefabField = element.Q<ObjectField>("prefab-field");

			// Display address as read-only label
			var prefab = prefabProperty.objectReferenceValue as GameObject;
			addressLabel.text = prefab != null ? $"[{prefab.name}]" : "[No Prefab]";

			prefabField.Unbind();
			prefabField.BindProperty(prefabProperty);

			prefabField.RegisterValueChangedCallback(evt =>
			{
				if (evt.newValue is GameObject newPrefab)
				{
					// Automatically update address from prefab name
					addressProperty.stringValue = newPrefab.name;
					addressProperty.serializedObject.ApplyModifiedProperties();
					addressLabel.text = $"[{newPrefab.name}]";
				}
				else
				{
					addressLabel.text = "[No Prefab]";
				}
				SyncConfigs();
			});
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

