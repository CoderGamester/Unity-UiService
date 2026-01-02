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
		[SerializeField] private TMP_Text _explanationText;
		
		private IUiServiceInit _uiService;

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
			var fadingPresenter = await _uiService.LoadUiAsync<FadingPresenter>();
			var scalingPresenter = await _uiService.LoadUiAsync<ScalingPresenter>();
			var fullFeaturedPresenter = await _uiService.LoadUiAsync<FullFeaturedPresenter>();
			
			fadingPresenter.OnCloseRequested.AddListener(() => UpdateUiVisibility(false));
			scalingPresenter.OnCloseRequested.AddListener(() => UpdateUiVisibility(false));
			fullFeaturedPresenter.OnCloseRequested.AddListener(() => UpdateUiVisibility(false));

			UpdateUiVisibility(false);
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
		public async void OpenFadeUi()
		{
			await _uiService.OpenUiAsync<FadingPresenter>();
			UpdateUiVisibility(true);
		}

		/// <summary>
		/// Opens UI with ScaleFeature (scales in/out)
		/// </summary>
		public async void OpenScaleUi()
		{
			await _uiService.OpenUiAsync<ScalingPresenter>();
			UpdateUiVisibility(true);
		}

		/// <summary>
		/// Opens UI with all features combined
		/// </summary>
		public async void OpenAllFeaturesUi()
		{
			await _uiService.OpenUiAsync<FullFeaturedPresenter>();
			UpdateUiVisibility(true);
		}

		/// <summary>
		/// Closes all open UIs
		/// </summary>
		public void CloseAllUi()
		{
			_uiService.CloseAllUi();
			UpdateUiVisibility(false);
		}

		private void UpdateUiVisibility(bool isPresenterActive)
		{
			if (_explanationText != null)
			{
				_explanationText.gameObject.SetActive(!isPresenterActive);
			}
		}
	}
}

