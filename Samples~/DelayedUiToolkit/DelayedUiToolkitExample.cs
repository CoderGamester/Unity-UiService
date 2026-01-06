using UnityEngine;
using UnityEngine.UIElements;
using GameLovers.UiService;
using Cysharp.Threading.Tasks;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Example demonstrating delayed UI Toolkit presenters with TimeDelayFeature and AnimationDelayFeature.
	/// Uses UI Toolkit for both the example controls and the presenters.
	/// </summary>
	public class DelayedUiToolkitExample : MonoBehaviour
	{
		[SerializeField] private PrefabRegistryUiConfigs _uiConfigs;
		[SerializeField] private UIDocument _uiDocument;

		private IUiServiceInit _uiService;
		private Label _statusLabel;

		private async void Start()
		{
			// Initialize UI Service
			var loader = new PrefabRegistryUiAssetLoader(_uiConfigs);

			_uiService = new UiService(loader);
			_uiService.Init(_uiConfigs);
			
			// Setup button listeners from UIDocument
			var root = _uiDocument.rootVisualElement;
			
			_statusLabel = root.Q<Label>("status-label");
			
			root.Q<Button>("open-time-delayed-button")?.RegisterCallback<ClickEvent>(_ => OpenTimeDelayedUi());
			root.Q<Button>("open-animated-button")?.RegisterCallback<ClickEvent>(_ => OpenAnimatedUi());
			
			// Pre-load presenters and subscribe to close events
			var timeDelayedPresenter = await _uiService.LoadUiAsync<TimeDelayedUiToolkitPresenter>();
			var animatedPresenter = await _uiService.LoadUiAsync<AnimationDelayedUiToolkitPresenter>();
			
			timeDelayedPresenter.OnCloseRequested.AddListener(() => UpdateStatus("UI Closed but still in memory"));
			animatedPresenter.OnCloseRequested.AddListener(() => UpdateStatus("UI Closed but still in memory"));

			UpdateStatus("Ready");
		}

		private void OnDestroy()
		{
		}

		private async void OpenTimeDelayedUi()
		{
			await _uiService.OpenUiAsync<TimeDelayedUiToolkitPresenter>();
			UpdateStatus("Time Delayed UI Opened");
		}

		private async void OpenAnimatedUi()
		{
			await _uiService.OpenUiAsync<AnimationDelayedUiToolkitPresenter>();
			UpdateStatus("Animated UI Opened");
		}

		private void UpdateStatus(string message)
		{
			if (_statusLabel != null)
			{
				_statusLabel.text = message;
			}
		}
	}
}

