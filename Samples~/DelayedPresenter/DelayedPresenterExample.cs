using UnityEngine;
using UnityEngine.UI;
using GameLovers.UiService;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Example demonstrating delayed UI presenters with animations and time delays.
	/// Uses UI buttons for input to avoid dependency on any specific input system.
	/// </summary>
	public class DelayedPresenterExample : MonoBehaviour
	{
		[SerializeField] private UiConfigs _uiConfigs;
		
		[Header("UI Buttons")]
		[SerializeField] private Button _openTimeDelayedButton;
		[SerializeField] private Button _openAnimatedButton;
		[SerializeField] private Button _closeButton;
		
		private IUiService _uiService;

		private void Start()
		{
			// Initialize UI Service
			_uiService = new UiService();
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
			_uiService.OpenUi<DelayedUiExamplePresenter>();
		}

		/// <summary>
		/// Opens the animation-delayed UI presenter
		/// </summary>
		public void OpenAnimatedUi()
		{
			Debug.Log("Opening Animation-Delayed UI (watch for animation)...");
			_uiService.OpenUi<AnimatedUiExamplePresenter>();
		}

		/// <summary>
		/// Closes the currently active UI
		/// </summary>
		public void CloseActiveUi()
		{
			Debug.Log("Closing UI...");
			
			if (_uiService.IsUiOpen<DelayedUiExamplePresenter>())
			{
				_uiService.CloseUi<DelayedUiExamplePresenter>(destroy: false);
			}
			else if (_uiService.IsUiOpen<AnimatedUiExamplePresenter>())
			{
				_uiService.CloseUi<AnimatedUiExamplePresenter>(destroy: false);
			}
		}
	}
}
