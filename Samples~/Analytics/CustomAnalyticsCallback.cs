using UnityEngine;
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
}

