using UnityEngine;
using UnityEngine.UI;
using GameLovers.UiService;
using Cysharp.Threading.Tasks;
using TMPro;

namespace GameLovers.UiService.Examples
{
	/// <summary>
	/// Example implementation of custom analytics callback.
	/// This demonstrates how to integrate with external analytics services.
	/// </summary>
	public class CustomAnalyticsCallback : IUiAnalyticsCallback
	{
		public void OnUiLoaded(UiEventData data)
		{
			Debug.Log($"[CustomAnalytics] UI Loaded: {data.UiName} on layer {data.Layer} at {data.Timestamp}s");
			
			// Example: Send to your analytics service
			// AnalyticsService.TrackEvent("ui_loaded", new {
			//     ui_name = data.UiName,
			//     layer = data.Layer,
			//     timestamp = data.Timestamp
			// });
		}

		public void OnUiOpened(UiEventData data)
		{
			Debug.Log($"[CustomAnalytics] UI Opened: {data.UiName} on layer {data.Layer}");
			
			// Example: Send to your analytics service
			// AnalyticsService.TrackEvent("ui_opened", new {
			//     ui_name = data.UiName,
			//     layer = data.Layer
			// });
		}

		public void OnUiClosed(UiEventData data)
		{
			Debug.Log($"[CustomAnalytics] UI Closed: {data.UiName} (destroyed: {data.WasDestroyed})");
			
			// Example: Send to your analytics service
			// AnalyticsService.TrackEvent("ui_closed", new {
			//     ui_name = data.UiName,
			//     was_destroyed = data.WasDestroyed
			// });
		}

		public void OnUiUnloaded(UiEventData data)
		{
			Debug.Log($"[CustomAnalytics] UI Unloaded: {data.UiName}");
			
			// Example: Send to your analytics service
			// AnalyticsService.TrackEvent("ui_unloaded", new {
			//     ui_name = data.UiName
			// });
		}

		public void OnPerformanceMetricsUpdated(UiPerformanceMetrics metrics)
		{
			Debug.Log($"[CustomAnalytics] Performance Updated: {metrics.UiName} - " +
					  $"Load: {metrics.LoadDuration:F3}s, Open: {metrics.OpenDuration:F3}s, " +
					  $"Close: {metrics.CloseDuration:F3}s");
			
			// Example: Send performance metrics to your analytics service
			// AnalyticsService.TrackPerformance("ui_performance", new {
			//     ui_name = metrics.UiName,
			//     load_duration = metrics.LoadDuration,
			//     open_duration = metrics.OpenDuration,
			//     close_duration = metrics.CloseDuration
			// });
		}
	}

	/// <summary>
	/// Example demonstrating UI analytics integration.
	/// Uses UI buttons for input to avoid dependency on any specific input system.
	/// 
	/// Shows how to:
	/// - Create and configure a UiAnalytics instance
	/// - Set a custom callback for analytics events
	/// - Subscribe to analytics UnityEvents
	/// - View performance metrics
	/// </summary>
	public class AnalyticsExample : MonoBehaviour
	{
		[SerializeField] private PrefabRegistryUiConfigs _uiConfigs;

		[Header("UI Buttons")]
		[SerializeField] private Button _loadButton;
		[SerializeField] private Button _openButton;
		[SerializeField] private Button _closeButton;
		[SerializeField] private Button _viewSummaryButton;
		[SerializeField] private Button _clearDataButton;

		[Header("UI Elements")]
		[SerializeField] private TMP_Text _explanationText;
		
		private IUiServiceInit _uiService;
		private UiAnalytics _analytics;
		private CustomAnalyticsCallback _analyticsCallback;

		private async void Start()
		{
			// Create analytics instance
			_analytics = new UiAnalytics();
			
			// Set custom callback for analytics events
			_analyticsCallback = new CustomAnalyticsCallback();
			_analytics.SetCallback(_analyticsCallback);
			
			// Subscribe to analytics UnityEvents (alternative to callback)
			SubscribeToAnalyticsEvents();
			
			// Initialize UI Service with analytics
			var loader = new PrefabRegistryUiAssetLoader(_uiConfigs);

			_uiService = new UiService(loader, _analytics);
			_uiService.Init(_uiConfigs);
			
			// Setup button listeners
			_loadButton?.onClick.AddListener(LoadUi);
			_openButton?.onClick.AddListener(OpenUi);
			_closeButton?.onClick.AddListener(CloseUi);
			_viewSummaryButton?.onClick.AddListener(ViewPerformanceSummary);
			_clearDataButton?.onClick.AddListener(ClearAnalyticsData);
			
			// Pre-load presenter and subscribe to close events
			var presenter = await _uiService.LoadUiAsync<AnalyticsExamplePresenter>();
			presenter.OnCloseRequested.AddListener(() => UpdateUiVisibility(false));

			UpdateUiVisibility(false);
		}

		private void OnDestroy()
		{
			// Unsubscribe from events
			UnsubscribeFromAnalyticsEvents();
			
			// Remove button listeners
			_loadButton?.onClick.RemoveListener(LoadUi);
			_openButton?.onClick.RemoveListener(OpenUi);
			_closeButton?.onClick.RemoveListener(CloseUi);
			_viewSummaryButton?.onClick.RemoveListener(ViewPerformanceSummary);
			_clearDataButton?.onClick.RemoveListener(ClearAnalyticsData);
			
			// Dispose the UI service
			_uiService?.Dispose();
		}

		/// <summary>
		/// Loads the UI with analytics tracking
		/// </summary>
		public void LoadUi()
		{
			Debug.Log("Loading UI with analytics tracking...");
			_uiService.LoadUiAsync<AnalyticsExamplePresenter>().Forget();
		}

		/// <summary>
		/// Opens the UI with analytics tracking
		/// </summary>
		public async void OpenUi()
		{
			Debug.Log("Opening UI with analytics tracking...");
			await _uiService.OpenUiAsync<AnalyticsExamplePresenter>();
			UpdateUiVisibility(true);
		}

		/// <summary>
		/// Closes the UI with analytics tracking
		/// </summary>
		public void CloseUi()
		{
			Debug.Log("Closing UI with analytics tracking...");
			_uiService.CloseUi<AnalyticsExamplePresenter>(destroy: false);
			UpdateUiVisibility(false);
		}

		/// <summary>
		/// Displays performance summary and detailed metrics
		/// </summary>
		public void ViewPerformanceSummary()
		{
			Debug.Log("Displaying performance summary...");
			_analytics.LogPerformanceSummary();
			
			// Get specific metrics
			var metrics = _analytics.GetMetrics(typeof(AnalyticsExamplePresenter));
			Debug.Log($"\nDetailed metrics for AnalyticsExamplePresenter:");
			Debug.Log($"  Opens: {metrics.OpenCount}, Closes: {metrics.CloseCount}");
			Debug.Log($"  Lifetime: {metrics.TotalLifetime:F1}s");
		}

		/// <summary>
		/// Clears all analytics data
		/// </summary>
		public void ClearAnalyticsData()
		{
			Debug.Log("Clearing all analytics data...");
			_analytics.Clear();
		}

		private void SubscribeToAnalyticsEvents()
		{
			// Subscribe to UnityEvents from the analytics instance
			_analytics.OnUiLoaded.AddListener(OnUiLoadedEvent);
			_analytics.OnUiOpened.AddListener(OnUiOpenedEvent);
			_analytics.OnUiClosed.AddListener(OnUiClosedEvent);
			_analytics.OnUiUnloaded.AddListener(OnUiUnloadedEvent);
			_analytics.OnPerformanceMetricsUpdated.AddListener(OnPerformanceUpdatedEvent);
		}

		private void UnsubscribeFromAnalyticsEvents()
		{
			if (_analytics == null) return;
			
			_analytics.OnUiLoaded.RemoveListener(OnUiLoadedEvent);
			_analytics.OnUiOpened.RemoveListener(OnUiOpenedEvent);
			_analytics.OnUiClosed.RemoveListener(OnUiClosedEvent);
			_analytics.OnUiUnloaded.RemoveListener(OnUiUnloadedEvent);
			_analytics.OnPerformanceMetricsUpdated.RemoveListener(OnPerformanceUpdatedEvent);
		}

		private void OnUiLoadedEvent(UiEventData data)
		{
			Debug.Log($"[Event] UI Loaded: {data.UiName}");
		}

		private void OnUiOpenedEvent(UiEventData data)
		{
			Debug.Log($"[Event] UI Opened: {data.UiName}");
		}

		private void OnUiClosedEvent(UiEventData data)
		{
			Debug.Log($"[Event] UI Closed: {data.UiName}");
		}

		private void OnUiUnloadedEvent(UiEventData data)
		{
			Debug.Log($"[Event] UI Unloaded: {data.UiName}");
		}

		private void OnPerformanceUpdatedEvent(UiPerformanceMetrics metrics)
		{
			// This fires frequently, so we don't log every update
			// Debug.Log($"[Event] Performance Updated: {metrics.UiName}");
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

