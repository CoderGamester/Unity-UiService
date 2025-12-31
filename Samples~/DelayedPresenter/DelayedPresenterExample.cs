using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GameLovers.UiService;
using Cysharp.Threading.Tasks;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Example demonstrating delayed UI presenters with animations and time delays.
	/// Uses UI buttons for input to avoid dependency on any specific input system.
	/// </summary>
	public class DelayedPresenterExample : MonoBehaviour
	{
		[SerializeField] private PrefabRegistryUiConfigs _uiConfigs;

		[Header("UI Buttons")]
		[SerializeField] private Button _openTimeDelayedButton;
		[SerializeField] private Button _openAnimatedButton;
		[SerializeField] private Button _closeButton;

		[Header("UI Elements")]
		[SerializeField] private TMP_Text _explanationText;
		
		private IUiServiceInit _uiService;

		private void Start()
		{
			// Initialize UI Service
			var loader = new PrefabRegistryUiAssetLoader(_uiConfigs);

			_uiService = new UiService(loader);
			_uiService.Init(_uiConfigs);
			
			// Setup button listeners
			_openTimeDelayedButton?.onClick.AddListener(OpenTimeDelayedUi);
			_openAnimatedButton?.onClick.AddListener(OpenAnimatedUi);
			_closeButton?.onClick.AddListener(CloseActiveUi);

			// Initialize UI visibility state
			UpdateUiVisibility();
		}

		private void OnDestroy()
		{
			_openTimeDelayedButton?.onClick.RemoveListener(OpenTimeDelayedUi);
			_openAnimatedButton?.onClick.RemoveListener(OpenAnimatedUi);
			_closeButton?.onClick.RemoveListener(CloseActiveUi);
		}

		/// <summary>
		/// Opens the time-delayed UI presenter
		/// </summary>
		public void OpenTimeDelayedUi()
		{
			CloseActiveUi();
			_uiService.OpenUiAsync<DelayedUiExamplePresenter>().Forget();
			UpdateUiVisibility();
		}

		/// <summary>
		/// Opens the animation-delayed UI presenter
		/// </summary>
		public void OpenAnimatedUi()
		{
			CloseActiveUi();
			_uiService.OpenUiAsync<AnimatedUiExamplePresenter>().Forget();
			UpdateUiVisibility();
		}

		/// <summary>
		/// Closes the currently active UI
		/// </summary>
		public void CloseActiveUi()
		{
			if (_uiService.IsVisible<DelayedUiExamplePresenter>())
			{
				_uiService.CloseUi<DelayedUiExamplePresenter>(destroy: false);
			}
			else if (_uiService.IsVisible<AnimatedUiExamplePresenter>())
			{
				_uiService.CloseUi<AnimatedUiExamplePresenter>(destroy: false);
			}

			UpdateUiVisibility();
		}

		/// <summary>
		/// Updates the visibility of UI elements based on presenter state.
		/// Explanation text is shown when no presenter is active.
		/// Close button is shown only when a presenter is active.
		/// </summary>
		private void UpdateUiVisibility()
		{
			var isPresenterActive = _uiService.IsVisible<DelayedUiExamplePresenter>() || 
			                        _uiService.IsVisible<AnimatedUiExamplePresenter>();

			
			_explanationText.gameObject.SetActive(!isPresenterActive);
			_closeButton.gameObject.SetActive(isPresenterActive);
		}
	}
}
