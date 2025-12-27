using System;
using System.Collections.Generic;
using NSubstitute;
using UnityEngine.Events;

namespace GameLovers.UiService.Tests
{
	/// <summary>
	/// Hybrid analytics mock using NSubstitute for method call verification
	/// while providing real UnityEvent instances (which NSubstitute cannot mock).
	/// 
	/// Usage:
	///   var mockAnalytics = new MockAnalytics();
	///   // ... perform actions ...
	///   mockAnalytics.Substitute.Received(1).TrackLoadComplete(typeof(MyPresenter), Arg.Any&lt;int&gt;());
	/// </summary>
	public class MockAnalytics : IUiAnalytics
	{
		/// <summary>
		/// The underlying NSubstitute mock for verification.
		/// Use this for .Received() assertions.
		/// </summary>
		public IUiAnalytics Substitute { get; }

		// Real UnityEvent instances - NSubstitute cannot create these
		public UnityEvent<UiEventData> OnUiLoaded { get; } = new();
		public UnityEvent<UiEventData> OnUiOpened { get; } = new();
		public UnityEvent<UiEventData> OnUiClosed { get; } = new();
		public UnityEvent<UiEventData> OnUiUnloaded { get; } = new();
		public UnityEvent<UiPerformanceMetrics> OnPerformanceMetricsUpdated { get; } = new();

		private readonly Dictionary<Type, UiPerformanceMetrics> _metrics = new();

		public MockAnalytics()
		{
			Substitute = NSubstitute.Substitute.For<IUiAnalytics>();
		}

		public IReadOnlyDictionary<Type, UiPerformanceMetrics> PerformanceMetrics => _metrics;

		public void TrackLoadStart(Type uiType)
		{
			Substitute.TrackLoadStart(uiType);
		}

		public void TrackLoadComplete(Type uiType, int layer)
		{
			Substitute.TrackLoadComplete(uiType, layer);
		}

		public void TrackOpenStart(Type uiType)
		{
			Substitute.TrackOpenStart(uiType);
		}

		public void TrackOpenComplete(Type uiType, int layer)
		{
			Substitute.TrackOpenComplete(uiType, layer);
		}

		public void TrackCloseStart(Type uiType)
		{
			Substitute.TrackCloseStart(uiType);
		}

		public void TrackCloseComplete(Type uiType, int layer, bool destroyed)
		{
			Substitute.TrackCloseComplete(uiType, layer, destroyed);
		}

		public void TrackUnload(Type uiType, int layer)
		{
			Substitute.TrackUnload(uiType, layer);
		}

		public UiPerformanceMetrics GetMetrics(Type uiType)
		{
			return _metrics.TryGetValue(uiType, out var metrics) ? metrics : new UiPerformanceMetrics(uiType);
		}

		public void SetCallback(IUiAnalyticsCallback callback)
		{
			Substitute.SetCallback(callback);
		}

		public void Clear()
		{
			_metrics.Clear();
			Substitute.Clear();
		}

		public void LogPerformanceSummary()
		{
			Substitute.LogPerformanceSummary();
		}
	}
}

