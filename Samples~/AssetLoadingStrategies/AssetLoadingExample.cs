using System;
using UnityEngine;
using UnityEngine.UI;
using GameLovers.UiService;
using Cysharp.Threading.Tasks;

// ReSharper disable CheckNamespace

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Example demonstrating different UI asset loading strategies:
	/// PrefabRegistry, Addressables, and Resources.
	/// </summary>
	public class AssetLoadingExample : MonoBehaviour
	{
		public enum LoadingStrategy { PrefabRegistry, Addressables, Resources }

		[SerializeField] private LoadingStrategy _strategy = LoadingStrategy.PrefabRegistry;

		[Header("PrefabRegistry (works immediately)")]
		[SerializeField] private PrefabRegistryUiConfigs _prefabRegistryConfigs;

		[Header("Addressables (requires setup)")]
		[SerializeField] private AddressablesUiConfigs _addressablesConfigs;

		[Header("Resources (requires setup)")]
		[SerializeField] private ResourcesUiConfigs _resourcesConfigs;

		[Header("UI Buttons")]
		[SerializeField] private Button _loadButton;
		[SerializeField] private Button _openButton;
		[SerializeField] private Button _closeButton;
		[SerializeField] private Button _unloadButton;

		private IUiServiceInit _uiService;

		private void Start()
		{
			IUiAssetLoader loader = _strategy switch
			{
				LoadingStrategy.PrefabRegistry => new PrefabRegistryUiAssetLoader(_prefabRegistryConfigs),
				LoadingStrategy.Addressables => new AddressablesUiAssetLoader(),
				LoadingStrategy.Resources => new ResourcesUiAssetLoader(),
				_ => throw new ArgumentOutOfRangeException()
			};

			UiConfigs configs = _strategy switch
			{
				LoadingStrategy.PrefabRegistry => _prefabRegistryConfigs,
				LoadingStrategy.Addressables => _addressablesConfigs,
				LoadingStrategy.Resources => _resourcesConfigs,
				_ => throw new ArgumentOutOfRangeException()
			};

			// Initialize UI Service with the selected loader and configs
			_uiService = new UiService(loader);
			_uiService.Init(configs);

			// Setup button listeners
			_loadButton?.onClick.AddListener(LoadUi);
			_openButton?.onClick.AddListener(OpenUi);
			_closeButton?.onClick.AddListener(CloseUi);
			_unloadButton?.onClick.AddListener(UnloadUi);

			Debug.Log($"=== Asset Loading Example Started ({_strategy}) ===");
		}

		private void OnDestroy()
		{
			_loadButton?.onClick.RemoveListener(LoadUi);
			_openButton?.onClick.RemoveListener(OpenUi);
			_closeButton?.onClick.RemoveListener(CloseUi);
			_unloadButton?.onClick.RemoveListener(UnloadUi);
			
			_uiService?.Dispose();
		}

		private void LoadUi()
		{
			Debug.Log("Loading ExamplePresenter...");
			_uiService.LoadUiAsync<ExamplePresenter>().Forget();
		}

		private void OpenUi()
		{
			Debug.Log("Opening ExamplePresenter...");
			_uiService.OpenUiAsync<ExamplePresenter>().Forget();
		}

		private void CloseUi()
		{
			Debug.Log("Closing ExamplePresenter...");
			_uiService.CloseUi<ExamplePresenter>(destroy: false);
		}

		private void UnloadUi()
		{
			Debug.Log("Unloading ExamplePresenter...");
			_uiService.UnloadUi<ExamplePresenter>();
		}
	}
}

