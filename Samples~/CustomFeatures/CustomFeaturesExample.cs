using UnityEngine;
using UnityEngine.UI;
using GameLovers.UiService;
using Cysharp.Threading.Tasks;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Example demonstrating how to create and use custom presenter features.
	/// Uses UI buttons for input to avoid dependency on any specific input system.
	/// 
	/// Key concepts:
	/// - PresenterFeatureBase: Base class for features that hook into presenter lifecycle
	/// - Feature composition: Attach multiple features to a single presenter
	/// 
	/// This sample includes three custom features:
	/// 1. FadeFeature: Fades the UI in/out using CanvasGroup
	/// 2. SoundFeature: Plays sounds on open/close
	/// 3. ScaleFeature: Scales the UI in/out with animation
	/// </summary>
	public class CustomFeaturesExample : MonoBehaviour
	{
		[SerializeField] private UiConfigs _uiConfigs;

		[Header("Sample Prefabs")]
		[SerializeField] private GameObject[] _presenterPrefabs;
		
		[Header("UI Buttons")]
		[SerializeField] private Button _openFadeButton;
		[SerializeField] private Button _openScaleButton;
		[SerializeField] private Button _openAllFeaturesButton;
		[SerializeField] private Button _closeAllButton;
		
		private IUiServiceInit _uiService;

		private void Start()
		{
			// Initialize UI Service
			var loader = new PrefabRegistryUiAssetLoader();
			foreach (var prefab in _presenterPrefabs)
			{
				var presenter = prefab.GetComponent<UiPresenter>();
				loader.RegisterPrefab(presenter.GetType().Name, prefab);
			}

			_uiService = new UiService(loader);
			_uiService.Init(_uiConfigs);
			
			// Setup button listeners
			_openFadeButton?.onClick.AddListener(OpenFadeUi);
			_openScaleButton?.onClick.AddListener(OpenScaleUi);
			_openAllFeaturesButton?.onClick.AddListener(OpenAllFeaturesUi);
			_closeAllButton?.onClick.AddListener(CloseAllUi);
			
			Debug.Log("=== Custom Features Example Started ===");
			Debug.Log("This example demonstrates creating custom presenter features.");
			Debug.Log("");
			Debug.Log("Features are components that hook into the presenter lifecycle:");
			Debug.Log("  - OnPresenterInitialized: Called once when presenter is created");
			Debug.Log("  - OnPresenterOpening: Called before presenter becomes visible");
			Debug.Log("  - OnPresenterOpened: Called after presenter is visible");
			Debug.Log("  - OnPresenterClosing: Called before presenter is hidden");
			Debug.Log("  - OnPresenterClosed: Called after presenter is hidden");
			Debug.Log("");
			Debug.Log("Use the UI buttons to test different feature combinations.");
		}

		private void OnDestroy()
		{
			_openFadeButton?.onClick.RemoveListener(OpenFadeUi);
			_openScaleButton?.onClick.RemoveListener(OpenScaleUi);
			_openAllFeaturesButton?.onClick.RemoveListener(OpenAllFeaturesUi);
			_closeAllButton?.onClick.RemoveListener(CloseAllUi);
			
			_uiService?.Dispose();
		}

		/// <summary>
		/// Opens UI with FadeFeature (fades in/out)
		/// </summary>
		public void OpenFadeUi()
		{
			Debug.Log("Opening UI with FadeFeature...");
			_uiService.OpenUiAsync<FadingPresenter>().Forget();
		}

		/// <summary>
		/// Opens UI with ScaleFeature (scales in/out)
		/// </summary>
		public void OpenScaleUi()
		{
			Debug.Log("Opening UI with ScaleFeature...");
			_uiService.OpenUiAsync<ScalingPresenter>().Forget();
		}

		/// <summary>
		/// Opens UI with all features combined
		/// </summary>
		public void OpenAllFeaturesUi()
		{
			Debug.Log("Opening UI with all features...");
			_uiService.OpenUiAsync<FullFeaturedPresenter>().Forget();
		}

		/// <summary>
		/// Closes all open UIs
		/// </summary>
		public void CloseAllUi()
		{
			Debug.Log("Closing all UIs...");
			_uiService.CloseAllUi();
		}
	}
}
