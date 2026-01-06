using System;
using System.Collections.Generic;
using GameLovers.UiService;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

// ReSharper disable once CheckNamespace

namespace GameLoversEditor.UiService
{
	/// <summary>
	/// Implementation of <see cref="UiConfigsEditorBase{TSet}"/> that syncs with Unity Addressables.
	/// </summary>
	public abstract class AddressablesUiConfigsEditor<TSet> : UiConfigsEditorBase<TSet>
		where TSet : Enum
	{
		private Dictionary<string, string> _assetPathLookup;
		private List<string> _uiConfigsAddress;
		private Dictionary<string, Type> _uiTypesByAddress;

		protected override void SyncConfigs()
		{
			var assetList = GetAssetList();
			var configs = new List<UiConfig>();
			_uiConfigsAddress = new List<string>();
			_assetPathLookup = new Dictionary<string, string>();
			var existingConfigs = ScriptableObjectInstance.Configs;

			foreach (var asset in assetList)
			{
				if (AssetDatabase.GetMainAssetTypeAtPath(asset.AssetPath) != typeof(GameObject))
					continue;

				var uiPresenter = AssetDatabase.LoadAssetAtPath<UiPresenter>(asset.AssetPath);
				if (uiPresenter == null)
					continue;

				var sortingOrder = GetSortingOrder(uiPresenter);
				var existingConfigIndex = existingConfigs.FindIndex(c => c.Address == asset.address);
				var presenterType = uiPresenter.GetType();
				
				var config = new UiConfig
				{
					Address = asset.address,
					Layer = existingConfigIndex >= 0 && sortingOrder < 0 ? existingConfigs[existingConfigIndex].Layer : 
					        sortingOrder < 0 ? 0 : sortingOrder,
					UiType = presenterType,
					LoadSynchronously = Attribute.IsDefined(presenterType, typeof(LoadSynchronouslyAttribute))
				};

				configs.Add(config);
				_uiConfigsAddress.Add(asset.address);
				_assetPathLookup[asset.address] = asset.AssetPath;
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
			container.style.paddingTop = 2;
			container.style.paddingBottom = 2;

			var label = new Label();
			label.style.flexGrow = 1;
			label.style.paddingLeft = 5;
			label.style.unityTextAlign = TextAnchor.MiddleLeft;
			container.Add(label);

			var layerField = new IntegerField();
			layerField.style.width = 80;
			layerField.style.marginRight = 5;
			container.Add(layerField);

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

		private static List<AddressableAssetEntry> GetAssetList()
		{
			var assetList = new List<AddressableAssetEntry>();
			var assetsSettings = AddressableAssetSettingsDefaultObject.Settings;
			if (assetsSettings == null) return assetList;

			foreach (var settingsGroup in assetsSettings.groups)
			{
				if (settingsGroup.ReadOnly) continue;
				settingsGroup.GatherAllAssets(assetList, true, true, true);
			}

			return assetList;
		}
	}
}

