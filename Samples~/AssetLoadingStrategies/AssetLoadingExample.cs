using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GameLovers.UiService;
using Cysharp.Threading.Tasks;
using TMPro;

// ReSharper disable CheckNamespace

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Example demonstrating different UI asset loading strategies:
	/// PrefabRegistry, Addressables, and Resources.
	/// 
	/// STRATEGY SETUP GUIDE:
	/// 
	/// 1. PREFAB REGISTRY (works immediately - no setup required)
	///    - Uses direct prefab references stored in the config asset
	///    - Best for: Samples, prototyping, small projects
	/// 
	/// 2. RESOURCES (works immediately after sample import)
	///    - Loads prefabs from the Resources folder
	///    - The sample includes a Resources/ExamplePresenter.prefab ready to use
	///    - Address in config = path relative to Resources folder (e.g., "ExamplePresenter")
	/// 
	/// 3. ADDRESSABLES (requires additional setup)
	///    To make Addressables work:
	///    a) Open Window > Asset Management > Addressables > Groups
	///    b) If no Addressable Groups exist, click "Create Addressables Settings"
	///    c) Find the ExamplePresenter.prefab in the sample folder
	///    d) Check the "Addressable" checkbox in the prefab's Inspector
	///    e) Set the address to "ExamplePresenter" (must match the config)
	///    f) For Play Mode: Window > Asset Management > Addressables > Groups > Play Mode Script
	///       - Select "Use Asset Database (fastest)" for editor testing
	///    g) For builds: Build the Addressables catalog before building the player
	/// </summary>
	public class AssetLoadingExample : MonoBehaviour
	{
		public enum LoadingStrategy { PrefabRegistry, Addressables, Resources }

		[Header("Strategy Selection")]
		[SerializeField] private LoadingStrategy _initialStrategy = LoadingStrategy.PrefabRegistry;
		[SerializeField] private TMP_Dropdown _strategyDropdown;
		[SerializeField] private TMP_Text _statusText;

		[Header("PrefabRegistry (works immediately)")]
		[SerializeField] private PrefabRegistryUiConfigs _prefabRegistryConfigs;

		[Header("Addressables (requires setup - see class docs)")]
		[SerializeField] private AddressablesUiConfigs _addressablesConfigs;

		[Header("Resources (works immediately)")]
		[SerializeField] private ResourcesUiConfigs _resourcesConfigs;

		[Header("UI Buttons")]
		[SerializeField] private Button _loadButton;
		[SerializeField] private Button _openButton;
		[SerializeField] private Button _unloadButton;

		private IUiServiceInit _uiService;
		private LoadingStrategy _currentStrategy;

		private void Start()
		{
			InitializeDropdown();
			InitializeService(_initialStrategy);

			// Setup button listeners
			_loadButton?.onClick.AddListener(LoadUi);
			_openButton?.onClick.AddListener(OpenUi);
			_unloadButton?.onClick.AddListener(UnloadUi);
		}

		private void OnDestroy()
		{
			_loadButton?.onClick.RemoveListener(LoadUi);
			_openButton?.onClick.RemoveListener(OpenUi);
			_unloadButton?.onClick.RemoveListener(UnloadUi);
			_strategyDropdown?.onValueChanged.RemoveListener(OnStrategyChanged);

			_uiService?.Dispose();
		}

		private void InitializeDropdown()
		{
			if (_strategyDropdown == null) return;

			_strategyDropdown.ClearOptions();
			var options = new List<string>
			{
				"PrefabRegistry (Ready)",
				"Addressables (Setup Required)",
				"Resources (Ready)"
			};
			_strategyDropdown.AddOptions(options);
			_strategyDropdown.value = (int)_initialStrategy;
			_strategyDropdown.onValueChanged.AddListener(OnStrategyChanged);
		}

		private void OnStrategyChanged(int index)
		{
			var newStrategy = (LoadingStrategy)index;
			if (newStrategy == _currentStrategy) return;

			// Dispose current service
			_uiService?.Dispose();
			_uiService = null;

			// Initialize with new strategy
			InitializeService(newStrategy);
		}

		private void InitializeService(LoadingStrategy strategy)
		{
			_currentStrategy = strategy;

			try
			{
				IUiAssetLoader loader = strategy switch
				{
					LoadingStrategy.PrefabRegistry => new PrefabRegistryUiAssetLoader(_prefabRegistryConfigs),
					LoadingStrategy.Addressables => new AddressablesUiAssetLoader(),
					LoadingStrategy.Resources => new ResourcesUiAssetLoader(),
					_ => throw new ArgumentOutOfRangeException()
				};

				UiConfigs configs = strategy switch
				{
					LoadingStrategy.PrefabRegistry => _prefabRegistryConfigs,
					LoadingStrategy.Addressables => _addressablesConfigs,
					LoadingStrategy.Resources => _resourcesConfigs,
					_ => throw new ArgumentOutOfRangeException()
				};

				if (configs == null)
				{
					SetStatus($"Error: {strategy} config asset is not assigned!", true);
					return;
				}

				// Initialize UI Service with the selected loader and configs
				_uiService = new UiService(loader);
				_uiService.Init(configs);

				SetStatus($"Strategy: {strategy} - Ready", false);
			}
			catch (Exception e)
			{
				SetStatus($"Error initializing {strategy}: {e.Message}", true);
				Debug.LogError($"[AssetLoadingExample] Failed to initialize {strategy}: {e}");
			}
		}

		private void SetStatus(string message, bool isError)
		{
			if (_statusText == null) return;

			_statusText.text = message;
			_statusText.color = isError ? Color.red : Color.white;
		}

		private void LoadUi()
		{
			if (_uiService == null)
			{
				SetStatus("Error: Service not initialized", true);
				return;
			}

			SetStatus($"Loading UI with {_currentStrategy}...", false);
			LoadUiAsync().Forget();
		}

		private async UniTaskVoid LoadUiAsync()
		{
			try
			{
				await _uiService.LoadUiAsync<ExamplePresenter>();
				SetStatus($"UI loaded successfully ({_currentStrategy})", false);
			}
			catch (Exception e)
			{
				SetStatus($"Load failed: {e.Message}", true);
				Debug.LogError($"[AssetLoadingExample] Load failed: {e}");
			}
		}

		private void OpenUi()
		{
			if (_uiService == null)
			{
				SetStatus("Error: Service not initialized", true);
				return;
			}

			OpenUiAsync().Forget();
		}

		private async UniTaskVoid OpenUiAsync()
		{
			try
			{
				await _uiService.OpenUiAsync<ExamplePresenter>();
				SetStatus($"UI opened ({_currentStrategy})", false);
			}
			catch (Exception e)
			{
				SetStatus($"Open failed: {e.Message}", true);
				Debug.LogError($"[AssetLoadingExample] Open failed: {e}");
			}
		}

		private void UnloadUi()
		{
			if (_uiService == null)
			{
				SetStatus("Error: Service not initialized", true);
				return;
			}

			try
			{
				_uiService.UnloadUi<ExamplePresenter>();
				SetStatus($"UI unloaded ({_currentStrategy})", false);
			}
			catch (Exception e)
			{
				SetStatus($"Unload failed: {e.Message}", true);
				Debug.LogError($"[AssetLoadingExample] Unload failed: {e}");
			}
		}
	}
}
