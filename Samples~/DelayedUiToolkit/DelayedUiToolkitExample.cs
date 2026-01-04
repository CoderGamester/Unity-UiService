using UnityEngine;
using UnityEngine.UIElements;
using GameLovers.UiService;
using Cysharp.Threading.Tasks;
using TMPro;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Example demonstrating delayed UI Toolkit presenters.
	/// Uses UI Toolkit for both the example controls and the presenters.
	/// </summary>
	public class DelayedUiToolkitExample : MonoBehaviour
	{
		[SerializeField] private PrefabRegistryUiConfigs _uiConfigs;
		[SerializeField] private UIDocument _uiDocument;

		[Header("UI Elements")]
		[SerializeField] private TMP_Text _explanationText;

		private IUiServiceInit _uiService;

		private async void Start()
		{
			// Initialize UI Service
			var loader = new PrefabRegistryUiAssetLoader(_uiConfigs);

			_uiService = new UiService(loader);
			_uiService.Init(_uiConfigs);
			
			// Setup button listeners from UIDocument
			var root = _uiDocument.rootVisualElement;
			
			root.Q<Button>("open-time-delayed-button")?.RegisterCallback<ClickEvent>(_ => OpenTimeDelayedUi());
			root.Q<Button>("open-animated-button")?.RegisterCallback<ClickEvent>(_ => OpenAnimatedUi());
			root.Q<Button>("close-button")?.RegisterCallback<ClickEvent>(_ => CloseActiveUi());
			
			// Pre-load presenters and subscribe to close events
			var timeDelayedPresenter = await _uiService.LoadUiAsync<TimeDelayedUiToolkitPresenter>();
			var animatedPresenter = await _uiService.LoadUiAsync<AnimationDelayedUiToolkitPresenter>();
			
			timeDelayedPresenter.OnCloseRequested.AddListener(() => UpdateUiVisibility(false));
			animatedPresenter.OnCloseRequested.AddListener(() => UpdateUiVisibility(false));

			UpdateUiVisibility(false);
		}

		private void OnDestroy()
		{
		}

		private async void OpenTimeDelayedUi()
		{
			await _uiService.OpenUiAsync<TimeDelayedUiToolkitPresenter>();
			UpdateUiVisibility(true);
		}

		private async void OpenAnimatedUi()
		{
			await _uiService.OpenUiAsync<AnimationDelayedUiToolkitPresenter>();
			UpdateUiVisibility(true);
		}

		private void CloseActiveUi()
		{
			if (_uiService.IsVisible<TimeDelayedUiToolkitPresenter>())
			{
				_uiService.CloseUi<TimeDelayedUiToolkitPresenter>(destroy: false);
			}
			else if (_uiService.IsVisible<AnimationDelayedUiToolkitPresenter>())
			{
				_uiService.CloseUi<AnimationDelayedUiToolkitPresenter>(destroy: false);
			}
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

