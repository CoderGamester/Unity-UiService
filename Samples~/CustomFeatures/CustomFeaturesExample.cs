using UnityEngine;
using GameLovers.UiService;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Example demonstrating how to create and use custom presenter features.
	/// 
	/// Key concepts:
	/// - IPresenterFeature: Interface for features that hook into presenter lifecycle
	/// - PresenterFeatureBase: Base class that provides common functionality
	/// - Feature composition: Attach multiple features to a single presenter
	/// 
	/// This sample includes three custom features:
	/// 1. FadeFeature: Fades the UI in/out using CanvasGroup
	/// 2. SoundFeature: Plays sounds on open/close
	/// 3. ScaleFeature: Scales the UI in/out with animation
	/// </summary>
	public class CustomFeaturesExample : MonoBehaviour
	{
		[SerializeField] private UiConfigs _uiConfigs;
		
		private IUiService _uiService;

		private void Start()
		{
			// Initialize UI Service
			_uiService = new UiService();
			_uiService.Init(_uiConfigs);
			
			Debug.Log("=== Custom Features Example Started ===");
			Debug.Log("This example demonstrates creating custom presenter features.");
			Debug.Log("");
			Debug.Log("Features are components that hook into the presenter lifecycle:");
			Debug.Log("  - OnPresenterInitialized: Called once when presenter is created");
			Debug.Log("  - OnPresenterOpening: Called before presenter becomes visible");
			Debug.Log("  - OnPresenterOpened: Called after presenter is visible");
			Debug.Log("  - OnPresenterClosing: Called before presenter is hidden");
			Debug.Log("  - OnPresenterClosed: Called after presenter is hidden");
			Debug.Log("");
			Debug.Log("Press 1: Open UI with FadeFeature (fades in/out)");
			Debug.Log("Press 2: Open UI with ScaleFeature (scales in/out)");
			Debug.Log("Press 3: Open UI with all features combined");
			Debug.Log("Press 4: Close all UIs");
		}

		private void OnDestroy()
		{
			_uiService?.Dispose();
		}

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.Alpha1))
			{
				Debug.Log("Opening UI with FadeFeature...");
				_uiService.OpenUiAsync<FadingPresenter>();
			}
			else if (Input.GetKeyDown(KeyCode.Alpha2))
			{
				Debug.Log("Opening UI with ScaleFeature...");
				_uiService.OpenUiAsync<ScalingPresenter>();
			}
			else if (Input.GetKeyDown(KeyCode.Alpha3))
			{
				Debug.Log("Opening UI with all features...");
				_uiService.OpenUiAsync<FullFeaturedPresenter>();
			}
			else if (Input.GetKeyDown(KeyCode.Alpha4))
			{
				Debug.Log("Closing all UIs...");
				_uiService.CloseAllUi();
			}
		}
	}
}

