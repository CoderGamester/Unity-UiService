using UnityEngine;
using UnityEngine.UI;
using GameLovers.UiService;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Example demonstrating the delayed UI Toolkit presenter functionality.
	/// This example shows how to combine UI Toolkit rendering with delay-based transitions.
	/// Uses UI buttons for input to avoid dependency on any specific input system.
	/// </summary>
	public class DelayedUiToolkitExample : MonoBehaviour
	{
		[SerializeField] private UiConfigs _uiConfigs;
		
		[Header("UI Buttons")]
		[SerializeField] private Button _openTimeDelayedButton;
		[SerializeField] private Button _openAnimationDelayedButton;
		[SerializeField] private Button _closeAllButton;

		private IUiService _uiService;

		private void Start()
		{
			// Initialize UI Service
			_uiService = new UiService();
			_uiService.Init(_uiConfigs);
			
			// Setup button listeners
			_openTimeDelayedButton?.onClick.AddListener(OpenTimeDelayedUiToolkit);
			_openAnimationDelayedButton?.onClick.AddListener(OpenAnimationDelayedUiToolkit);
			_closeAllButton?.onClick.AddListener(CloseAllUis);

			Debug.Log("=== Delayed UI Toolkit Example ===");
			Debug.Log("Use the UI buttons to test different delay scenarios:");
			Debug.Log("  Time Delayed: Opens UI Toolkit with time-based delay");
			Debug.Log("  Animation Delayed: Opens UI Toolkit with animation-based delay and data");
			Debug.Log("  Close All: Closes all open UIs");
		}

		private void OnDestroy()
		{
			_openTimeDelayedButton?.onClick.RemoveListener(OpenTimeDelayedUiToolkit);
			_openAnimationDelayedButton?.onClick.RemoveListener(OpenAnimationDelayedUiToolkit);
			_closeAllButton?.onClick.RemoveListener(CloseAllUis);
		}

		/// <summary>
		/// Opens a UI Toolkit presenter with time-based delays
		/// </summary>
		public async void OpenTimeDelayedUiToolkit()
		{
			Debug.Log("Opening Time-Delayed UI Toolkit Presenter...");
			await _uiService.OpenUiAsync<TimeDelayedUiToolkitPresenter>();
			Debug.Log("Time-Delayed UI Toolkit Presenter opened!");
		}

		/// <summary>
		/// Opens a UI Toolkit presenter with animation-based delays and data
		/// </summary>
		public async void OpenAnimationDelayedUiToolkit()
		{
			var data = new UiToolkitExampleData
			{
				Title = "Animated UI Toolkit",
				Message = "This UI uses animation delays!",
				Score = Random.Range(100, 999)
			};

			Debug.Log($"Opening Animation-Delayed UI Toolkit with data: {data.Title}");
			await _uiService.OpenUiAsync<AnimationDelayedUiToolkitPresenter, UiToolkitExampleData>(data);
			Debug.Log("Animation-Delayed UI Toolkit Presenter opened!");
		}

		/// <summary>
		/// Closes all open UIs
		/// </summary>
		public void CloseAllUis()
		{
			Debug.Log("Closing all UIs...");
			_uiService.CloseUi<TimeDelayedUiToolkitPresenter>();
			_uiService.CloseUi<AnimationDelayedUiToolkitPresenter>();
		}
	}
}
