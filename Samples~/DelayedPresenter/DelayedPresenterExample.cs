using UnityEngine;
using UnityEngine.Events;
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
		
		private IUiServiceInit _uiService;

		private async void Start()
		{
			// Initialize UI Service
			var loader = new PrefabRegistryUiAssetLoader(_uiConfigs);

			_uiService = new UiService(loader);
			_uiService.Init(_uiConfigs);
			
			// Setup button listeners
			_openTimeDelayedButton?.onClick.AddListener(OpenTimeDelayedUi);
			_openAnimatedButton?.onClick.AddListener(OpenAnimatedUi);
		}

		private void OnDestroy()
		{
			_openTimeDelayedButton?.onClick.RemoveListener(OpenTimeDelayedUi);
			_openAnimatedButton?.onClick.RemoveListener(OpenAnimatedUi);
		}

		/// <summary>
		/// Opens the time-delayed UI presenter
		/// </summary>
		public async void OpenTimeDelayedUi()
		{
			CloseActiveUi();
			await _uiService.OpenUiAsync<DelayedUiExamplePresenter>();
		}

		/// <summary>
		/// Opens the animation-delayed UI presenter
		/// </summary>
		public async void OpenAnimatedUi()
		{
			CloseActiveUi();
			await _uiService.OpenUiAsync<AnimatedUiExamplePresenter>();
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
		}
	}
}
