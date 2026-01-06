using UnityEngine;
using UnityEngine.UI;
using GameLovers.UiService;
using Cysharp.Threading.Tasks;
using TMPro;

namespace GameLovers.UiService.Examples
{
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
		[SerializeField] private Button _viewSummaryButton;
		[SerializeField] private Button _clearDataButton;

		[Header("UI Elements")]
		[SerializeField] private TMP_Text _statusText;
		
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
			_viewSummaryButton?.onClick.AddListener(ViewPerformanceSummary);
			_clearDataButton?.onClick.AddListener(ClearAnalyticsData);
			
			// Pre-load presenter and subscribe to close events
			var presenter = await _uiService.LoadUiAsync<AnalyticsExamplePresenter>();
			presenter.OnCloseRequested.AddListener(() => UpdateStatus("UI Closed"));
		}

		private void OnDestroy()
		{
			// Unsubscribe from events
			UnsubscribeFromAnalyticsEvents();
			
			// Remove button listeners
			_loadButton?.onClick.RemoveListener(LoadUi);
			_openButton?.onClick.RemoveListener(OpenUi);
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
			_uiService.LoadUiAsync<AnalyticsExamplePresenter>().Forget();
			UpdateStatus("UI Loaded");
		}

		/// <summary>
		/// Opens the UI with analytics tracking
		/// </summary>
		public async void OpenUi()
		{
			await _uiService.OpenUiAsync<AnalyticsExamplePresenter>();
			UpdateStatus("UI Opened");
		}

		/// <summary>
		/// Displays performance summary and detailed metrics
		/// </summary>
		public void ViewPerformanceSummary()
		{
			_analytics.LogPerformanceSummary();
			
			// Get specific metrics
			var metrics = _analytics.GetMetrics(typeof(AnalyticsExamplePresenter));
			UpdateStatus($"Metrics: Opens={metrics.OpenCount}, Closes={metrics.CloseCount}, Lifetime={metrics.TotalLifetime:F1}s");
		}

		/// <summary>
		/// Clears all analytics data
		/// </summary>
		public void ClearAnalyticsData()
		{
			_analytics.Clear();
			UpdateStatus("Analytics data cleared");
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

		private void UpdateStatus(string message)
		{
			if (_statusText != null)
			{
				_statusText.text = message;
			}
		}
	}
}

