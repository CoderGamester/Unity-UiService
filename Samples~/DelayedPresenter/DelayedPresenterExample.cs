using UnityEngine;
using UnityEngine.UI;
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
		[SerializeField] private UiConfigs _uiConfigs;

		[Header("Sample Prefabs")]
		[SerializeField] private GameObject[] _presenterPrefabs;
		
		[Header("UI Buttons")]
		[SerializeField] private Button _openTimeDelayedButton;
		[SerializeField] private Button _openAnimatedButton;
		[SerializeField] 		private Button _closeButton;
		
		private IUiServiceInit _uiService;

		private void Start()
		{
			// Initialize UI Service
			var loader = new SampleUiAssetLoader();
			foreach (var prefab in _presenterPrefabs)
			{
				var presenter = prefab.GetComponent<UiPresenter>();
				loader.RegisterPrefab(presenter.GetType().Name, prefab);
			}

			_uiService = new UiService(loader);
			_uiService.Init(_uiConfigs);
			
			// Setup button listeners
			_openTimeDelayedButton?.onClick.AddListener(OpenTimeDelayedUi);
			_openAnimatedButton?.onClick.AddListener(OpenAnimatedUi);
			_closeButton?.onClick.AddListener(CloseActiveUi);
			
			Debug.Log("=== Delayed Presenter Example Started ===");
			Debug.Log("Use the UI buttons to interact with the example:");
			Debug.Log("  Time Delayed: Opens UI with a time-based delay");
			Debug.Log("  Animated: Opens UI with animation-based delay");
			Debug.Log("  Close: Closes the currently active UI");
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
			Debug.Log("Opening Time-Delayed UI (watch for delayed appearance)...");
			_uiService.OpenUiAsync<DelayedUiExamplePresenter>().Forget();
		}

		/// <summary>
		/// Opens the animation-delayed UI presenter
		/// </summary>
		public void OpenAnimatedUi()
		{
			Debug.Log("Opening Animation-Delayed UI (watch for animation)...");
			_uiService.OpenUiAsync<AnimatedUiExamplePresenter>().Forget();
		}

		/// <summary>
		/// Closes the currently active UI
		/// </summary>
		public void CloseActiveUi()
		{
			Debug.Log("Closing UI...");
			
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
