using UnityEngine;
using UnityEngine.UIElements;
using GameLovers.UiService;
using Cysharp.Threading.Tasks;

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

		private IUiServiceInit _uiService;

		private void Start()
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
			
			Debug.Log("=== Delayed UI Toolkit Example Started ===");
		}

		private void OnDestroy()
		{
			_uiService?.Dispose();
		}

		private void OpenTimeDelayedUi()
		{
			Debug.Log("Opening Time Delayed UI Toolkit UI...");
			_uiService.OpenUiAsync<TimeDelayedUiToolkitPresenter>().Forget();
		}

		private void OpenAnimatedUi()
		{
			Debug.Log("Opening Animated UI Toolkit UI...");
			_uiService.OpenUiAsync<AnimationDelayedUiToolkitPresenter>().Forget();
		}

		private void CloseActiveUi()
		{
			Debug.Log("Closing Active UI Toolkit UI...");
			if (_uiService.IsVisible<TimeDelayedUiToolkitPresenter>())
			{
				_uiService.CloseUi<TimeDelayedUiToolkitPresenter>(destroy: false);
			}
			else if (_uiService.IsVisible<AnimationDelayedUiToolkitPresenter>())
			{
				_uiService.CloseUi<AnimationDelayedUiToolkitPresenter>(destroy: false);
			}
		}
	}
}
