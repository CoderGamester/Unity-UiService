using UnityEngine;
using UnityEngine.UI;
using GameLovers.UiService;
using Cysharp.Threading.Tasks;
using TMPro;

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
		[SerializeField] private PrefabRegistryUiConfigs _uiConfigs;

		[Header("UI Buttons")]
		[SerializeField] private Button _openFadeButton;
		[SerializeField] private Button _openScaleButton;
		[SerializeField] private Button _openAllFeaturesButton;
		[SerializeField] private Button _closeAllButton;

		[Header("UI Elements")]
		[SerializeField] private TMP_Text _statusText;
		
		private IUiServiceInit _uiService;
		private FadingPresenter _fadingPresenter;
		private ScalingPresenter _scalingPresenter;
		private FullFeaturedPresenter _fullFeaturedPresenter;

		private async void Start()
		{
			// Initialize UI Service
			var loader = new PrefabRegistryUiAssetLoader(_uiConfigs);

			_uiService = new UiService(loader);
			_uiService.Init(_uiConfigs);
			
			// Setup button listeners
			_openFadeButton?.onClick.AddListener(OpenFadeUi);
			_openScaleButton?.onClick.AddListener(OpenScaleUi);
			_openAllFeaturesButton?.onClick.AddListener(OpenAllFeaturesUi);
			_closeAllButton?.onClick.AddListener(CloseAllUi);
			
			// Pre-load presenters and subscribe to close events
			_fadingPresenter = await _uiService.LoadUiAsync<FadingPresenter>();
			_scalingPresenter = await _uiService.LoadUiAsync<ScalingPresenter>();
			_fullFeaturedPresenter = await _uiService.LoadUiAsync<FullFeaturedPresenter>();
			
			_fadingPresenter.OnCloseRequested.AddListener(OnPresenterCloseRequested);
			_scalingPresenter.OnCloseRequested.AddListener(OnPresenterCloseRequested);
			_fullFeaturedPresenter.OnCloseRequested.AddListener(OnPresenterCloseRequested);

			UpdateStatus("Ready - Select a feature to demo");
		}

		private void OnDestroy()
		{
			_openFadeButton?.onClick.RemoveListener(OpenFadeUi);
			_openScaleButton?.onClick.RemoveListener(OpenScaleUi);
			_openAllFeaturesButton?.onClick.RemoveListener(OpenAllFeaturesUi);
			_closeAllButton?.onClick.RemoveListener(CloseAllUi);
			
			_fadingPresenter?.OnCloseRequested.RemoveListener(OnPresenterCloseRequested);
			_scalingPresenter?.OnCloseRequested.RemoveListener(OnPresenterCloseRequested);
			_fullFeaturedPresenter?.OnCloseRequested.RemoveListener(OnPresenterCloseRequested);
		}

		/// <summary>
		/// Opens UI with FadeFeature (fades in/out)
		/// </summary>
		public async void OpenFadeUi()
		{
			await _uiService.OpenUiAsync<FadingPresenter>();
			UpdateStatus("Fade UI Opened");
		}

		/// <summary>
		/// Opens UI with ScaleFeature (scales in/out)
		/// </summary>
		public async void OpenScaleUi()
		{
			await _uiService.OpenUiAsync<ScalingPresenter>();
			UpdateStatus("Scale UI Opened");
		}

		/// <summary>
		/// Opens UI with all features combined
		/// </summary>
		public async void OpenAllFeaturesUi()
		{
			await _uiService.OpenUiAsync<FullFeaturedPresenter>();
			UpdateStatus("All Full Featured UI Opened");
		}

		/// <summary>
		/// Closes all open UIs
		/// </summary>
		public void CloseAllUi()
		{
			_uiService.CloseAllUi();
			UpdateStatus("All UIs closed");
		}

		private void OnPresenterCloseRequested()
		{
			UpdateStatus("UI Closed");
		}

		private void UpdateStatus(string message)
		{
			if (_statusText != null)
			{
				_statusText.text = message;
			}
		}
	}
}

