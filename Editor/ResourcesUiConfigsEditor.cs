using System;
using System.Collections.Generic;
using System.IO;
using GameLovers.UiService;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

// ReSharper disable once CheckNamespace

namespace GameLoversEditor.UiService
{
	/// <summary>
	/// Implementation of <see cref="UiConfigsEditorBase{TSet}"/> that syncs with the Resources folder.
	/// </summary>
	public abstract class ResourcesUiConfigsEditor<TSet> : UiConfigsEditorBase<TSet>
		where TSet : Enum
	{
		private Dictionary<string, string> _assetPathLookup;
		private List<string> _uiConfigsAddress;
		private Dictionary<string, Type> _uiTypesByAddress;

		protected override void SyncConfigs()
		{
			var configs = new List<UiConfig>();
			_uiConfigsAddress = new List<string>();
			_assetPathLookup = new Dictionary<string, string>();
			var existingConfigs = ScriptableObjectInstance.Configs;

			// Scan all Resources folders
			var guids = AssetDatabase.FindAssets("t:Prefab");
			foreach (var guid in guids)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				if (!path.Contains("/Resources/")) continue;

				var uiPresenter = AssetDatabase.LoadAssetAtPath<UiPresenter>(path);
				if (uiPresenter == null) continue;

				// Get relative path within Resources folder
				var resourcesPath = GetResourcesPath(path);
				var sortingOrder = GetSortingOrder(uiPresenter);
				var existingConfigIndex = existingConfigs.FindIndex(c => c.Address == resourcesPath);
				var presenterType = uiPresenter.GetType();

				var config = new UiConfig
				{
					Address = resourcesPath,
					Layer = existingConfigIndex >= 0 && sortingOrder < 0 ? existingConfigs[existingConfigIndex].Layer : 
					        sortingOrder < 0 ? 0 : sortingOrder,
					UiType = presenterType,
					LoadSynchronously = Attribute.IsDefined(presenterType, typeof(LoadSynchronouslyAttribute))
				};

				configs.Add(config);
				_uiConfigsAddress.Add(resourcesPath);
				_assetPathLookup[resourcesPath] = path;
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
			AssetDatabase.SaveAssets();
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

		private string GetResourcesPath(string fullPath)
		{
			var resourcesIndex = fullPath.IndexOf("/Resources/", StringComparison.Ordinal);
			if (resourcesIndex == -1) return fullPath;

			var path = fullPath.Substring(resourcesIndex + 11); // Skip "/Resources/"
			return Path.ChangeExtension(path, null); // Remove extension
		}

		private int GetSortingOrder(UiPresenter presenter)
		{
			if (presenter.TryGetComponent<Canvas>(out var canvas)) return canvas.sortingOrder;
			if (presenter.TryGetComponent<UnityEngine.UIElements.UIDocument>(out var document)) return (int)document.sortingOrder;
			return -1;
		}
	}
}

