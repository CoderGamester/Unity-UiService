using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace GameLovers.UiService.Tests.PlayMode
{
	/// <summary>
	/// Analytics mock with internal call tracking for verification.
	/// Works in both EditMode and PlayMode tests (no NSubstitute dependency).
	/// 
	/// Usage:
	///   var mockAnalytics = new MockAnalytics();
	///   // ... perform actions ...
	///   mockAnalytics.VerifyLoadStartCalled(typeof(MyPresenter), times: 1);
	///   mockAnalytics.VerifyLoadCompleteCalled(typeof(MyPresenter), layer: 0, times: 1);
	/// </summary>
	public class MockAnalytics : IUiAnalytics
	{
		/// <summary>
		/// Record of a method call with its arguments
		/// </summary>
		public struct CallRecord
		{
			public Type UiType;
			public int Layer;
			public bool Destroyed;

			public CallRecord(Type uiType, int layer = 0, bool destroyed = false)
			{
				UiType = uiType;
				Layer = layer;
				Destroyed = destroyed;
			}
		}

		// Call tracking lists
		private readonly List<CallRecord> _loadStartCalls = new();
		private readonly List<CallRecord> _loadCompleteCalls = new();
		private readonly List<CallRecord> _openStartCalls = new();
		private readonly List<CallRecord> _openCompleteCalls = new();
		private readonly List<CallRecord> _closeStartCalls = new();
		private readonly List<CallRecord> _closeCompleteCalls = new();
		private readonly List<CallRecord> _unloadCalls = new();

		// Real UnityEvent instances
		public UnityEvent<UiEventData> OnUiLoaded { get; } = new();
		public UnityEvent<UiEventData> OnUiOpened { get; } = new();
		public UnityEvent<UiEventData> OnUiClosed { get; } = new();
		public UnityEvent<UiEventData> OnUiUnloaded { get; } = new();
		public UnityEvent<UiPerformanceMetrics> OnPerformanceMetricsUpdated { get; } = new();

		private readonly Dictionary<Type, UiPerformanceMetrics> _metrics = new();

		public IReadOnlyDictionary<Type, UiPerformanceMetrics> PerformanceMetrics => _metrics;

		public void TrackLoadStart(Type uiType)
		{
			_loadStartCalls.Add(new CallRecord(uiType));
		}

		public void TrackLoadComplete(Type uiType, int layer)
		{
			_loadCompleteCalls.Add(new CallRecord(uiType, layer));
		}

		public void TrackOpenStart(Type uiType)
		{
			_openStartCalls.Add(new CallRecord(uiType));
		}

		public void TrackOpenComplete(Type uiType, int layer)
		{
			_openCompleteCalls.Add(new CallRecord(uiType, layer));
		}

		public void TrackCloseStart(Type uiType)
		{
			_closeStartCalls.Add(new CallRecord(uiType));
		}

		public void TrackCloseComplete(Type uiType, int layer, bool destroyed)
		{
			_closeCompleteCalls.Add(new CallRecord(uiType, layer, destroyed));
		}

		public void TrackUnload(Type uiType, int layer)
		{
			_unloadCalls.Add(new CallRecord(uiType, layer));
		}

		public UiPerformanceMetrics GetMetrics(Type uiType)
		{
			return _metrics.TryGetValue(uiType, out var metrics) ? metrics : new UiPerformanceMetrics(uiType);
		}

		public void SetCallback(IUiAnalyticsCallback callback)
		{
			// No-op for mock
		}

		public void Clear()
		{
			_metrics.Clear();
			ClearCallRecords();
		}

		public void LogPerformanceSummary()
		{
			// No-op for mock
		}

		/// <summary>
		/// Clears all call records for fresh verification
		/// </summary>
		public void ClearCallRecords()
		{
			_loadStartCalls.Clear();
			_loadCompleteCalls.Clear();
			_openStartCalls.Clear();
			_openCompleteCalls.Clear();
			_closeStartCalls.Clear();
			_closeCompleteCalls.Clear();
			_unloadCalls.Clear();
		}

		/// <summary>
		/// Gets the number of times TrackLoadStart was called for the specified type
		/// </summary>
		public int GetLoadStartCallCount(Type uiType = null)
		{
			return CountCalls(_loadStartCalls, uiType);
		}

		/// <summary>
		/// Gets the number of times TrackLoadComplete was called for the specified type
		/// </summary>
		public int GetLoadCompleteCallCount(Type uiType = null)
		{
			return CountCalls(_loadCompleteCalls, uiType);
		}

		/// <summary>
		/// Gets the number of times TrackOpenStart was called for the specified type
		/// </summary>
		public int GetOpenStartCallCount(Type uiType = null)
		{
			return CountCalls(_openStartCalls, uiType);
		}

		/// <summary>
		/// Gets the number of times TrackOpenComplete was called for the specified type
		/// </summary>
		public int GetOpenCompleteCallCount(Type uiType = null)
		{
			return CountCalls(_openCompleteCalls, uiType);
		}

		/// <summary>
		/// Gets the number of times TrackCloseStart was called for the specified type
		/// </summary>
		public int GetCloseStartCallCount(Type uiType = null)
		{
			return CountCalls(_closeStartCalls, uiType);
		}

		/// <summary>
		/// Gets the number of times TrackCloseComplete was called for the specified type
		/// </summary>
		public int GetCloseCompleteCallCount(Type uiType = null)
		{
			return CountCalls(_closeCompleteCalls, uiType);
		}

		/// <summary>
		/// Gets the number of times TrackUnload was called for the specified type
		/// </summary>
		public int GetUnloadCallCount(Type uiType = null)
		{
			return CountCalls(_unloadCalls, uiType);
		}

		/// <summary>
		/// Verifies TrackLoadStart was called the expected number of times
		/// </summary>
		public void VerifyLoadStartCalled(Type uiType, int times)
		{
			var actual = GetLoadStartCallCount(uiType);
			if (actual != times)
			{
				throw new AssertionException(
					$"Expected TrackLoadStart({uiType.Name}) to be called {times} time(s), but was called {actual} time(s).");
			}
		}

		/// <summary>
		/// Verifies TrackLoadComplete was called the expected number of times
		/// </summary>
		public void VerifyLoadCompleteCalled(Type uiType, int times)
		{
			var actual = GetLoadCompleteCallCount(uiType);
			if (actual != times)
			{
				throw new AssertionException(
					$"Expected TrackLoadComplete({uiType.Name}) to be called {times} time(s), but was called {actual} time(s).");
			}
		}

		/// <summary>
		/// Verifies TrackOpenStart was called the expected number of times
		/// </summary>
		public void VerifyOpenStartCalled(Type uiType, int times)
		{
			var actual = GetOpenStartCallCount(uiType);
			if (actual != times)
			{
				throw new AssertionException(
					$"Expected TrackOpenStart({uiType.Name}) to be called {times} time(s), but was called {actual} time(s).");
			}
		}

		/// <summary>
		/// Verifies TrackOpenComplete was called the expected number of times
		/// </summary>
		public void VerifyOpenCompleteCalled(Type uiType, int times)
		{
			var actual = GetOpenCompleteCallCount(uiType);
			if (actual != times)
			{
				throw new AssertionException(
					$"Expected TrackOpenComplete({uiType.Name}) to be called {times} time(s), but was called {actual} time(s).");
			}
		}

		/// <summary>
		/// Verifies TrackCloseStart was called the expected number of times
		/// </summary>
		public void VerifyCloseStartCalled(Type uiType, int times)
		{
			var actual = GetCloseStartCallCount(uiType);
			if (actual != times)
			{
				throw new AssertionException(
					$"Expected TrackCloseStart({uiType.Name}) to be called {times} time(s), but was called {actual} time(s).");
			}
		}

		/// <summary>
		/// Verifies TrackCloseComplete was called the expected number of times
		/// </summary>
		public void VerifyCloseCompleteCalled(Type uiType, int times)
		{
			var actual = GetCloseCompleteCallCount(uiType);
			if (actual != times)
			{
				throw new AssertionException(
					$"Expected TrackCloseComplete({uiType.Name}) to be called {times} time(s), but was called {actual} time(s).");
			}
		}

		/// <summary>
		/// Verifies TrackUnload was called the expected number of times
		/// </summary>
		public void VerifyUnloadCalled(Type uiType, int times)
		{
			var actual = GetUnloadCallCount(uiType);
			if (actual != times)
			{
				throw new AssertionException(
					$"Expected TrackUnload({uiType.Name}) to be called {times} time(s), but was called {actual} time(s).");
			}
		}

		private int CountCalls(List<CallRecord> calls, Type uiType)
		{
			if (uiType == null)
			{
				return calls.Count;
			}

			var count = 0;
			foreach (var call in calls)
			{
				if (call.UiType == uiType)
				{
					count++;
				}
			}
			return count;
		}
	}

	/// <summary>
	/// Custom assertion exception for verification failures
	/// </summary>
	public class AssertionException : Exception
	{
		public AssertionException(string message) : base(message) { }
	}
}
