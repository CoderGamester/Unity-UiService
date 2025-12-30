using UnityEngine;
using UnityEngine.UI;
using GameLovers.UiService;

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
		[SerializeField] private UiConfigs _uiConfigs;
		
		[Header("UI Buttons")]
		[SerializeField] private Button _loadButton;
		[SerializeField] private Button _openButton;
		[SerializeField] private Button _closeButton;
		[SerializeField] private Button _viewSummaryButton;
		[SerializeField] private Button _clearDataButton;
		
		private UiService _uiService;
		private UiAnalytics _analytics;
		private CustomAnalyticsCallback _analyticsCallback;

		private void Start()
		{
			// Create analytics instance
			_analytics = new UiAnalytics();
			
			// Set custom callback for analytics events
			_analyticsCallback = new CustomAnalyticsCallback();
			_analytics.SetCallback(_analyticsCallback);
			
			// Subscribe to analytics UnityEvents (alternative to callback)
			SubscribeToAnalyticsEvents();
			
			// Initialize UI Service with analytics
			// Note: Pass analytics to the UiService constructor
			_uiService = new UiService(new UiAssetLoader(), _analytics);
			_uiService.Init(_uiConfigs);
			
			// Setup button listeners
			_loadButton?.onClick.AddListener(LoadUi);
			_openButton?.onClick.AddListener(OpenUi);
			_closeButton?.onClick.AddListener(CloseUi);
			_viewSummaryButton?.onClick.AddListener(ViewPerformanceSummary);
			_clearDataButton?.onClick.AddListener(ClearAnalyticsData);
			
			Debug.Log("=== Analytics Example Started ===");
			Debug.Log("Use the UI buttons to test analytics tracking:");
			Debug.Log("  Load: Load UI and track load time");
			Debug.Log("  Open: Open UI and track open time");
			Debug.Log("  Close: Close UI and track close time");
			Debug.Log("  View Summary: Display performance metrics");
			Debug.Log("  Clear Data: Reset all analytics data");
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
			_uiService.LoadUiAsync<BasicUiExamplePresenter>();
		}

		/// <summary>
		/// Opens the UI with analytics tracking
		/// </summary>
		public void OpenUi()
		{
			Debug.Log("Opening UI with analytics tracking...");
			_uiService.OpenUiAsync<BasicUiExamplePresenter>();
		}

		/// <summary>
		/// Closes the UI with analytics tracking
		/// </summary>
		public void CloseUi()
		{
			Debug.Log("Closing UI with analytics tracking...");
			_uiService.CloseUi<BasicUiExamplePresenter>(destroy: false);
		}

		/// <summary>
		/// Displays performance summary and detailed metrics
		/// </summary>
		public void ViewPerformanceSummary()
		{
			Debug.Log("Displaying performance summary...");
			_analytics.LogPerformanceSummary();
			
			// Get specific metrics
			var metrics = _analytics.GetMetrics(typeof(BasicUiExamplePresenter));
			Debug.Log($"\nDetailed metrics for BasicUiExamplePresenter:");
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
	}
}
