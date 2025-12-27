using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace GameLovers.UiService.Tests
{
	/// <summary>
	/// Mock asset loader for testing without Addressables
	/// </summary>
	public class MockAssetLoader : IUiAssetLoader
	{
		private readonly Dictionary<string, GameObject> _prefabs = new();
		private readonly List<GameObject> _instantiatedObjects = new();
		
		public int InstantiateCallCount { get; private set; }
		public int UnloadCallCount { get; private set; }
		public bool ShouldFail { get; set; }
		public int SimulatedDelayMs { get; set; }

		public void RegisterPrefab(string address, GameObject prefab)
		{
			_prefabs[address] = prefab;
		}

		public void RegisterPrefab<T>(string address) where T : UiPresenter
		{
			_prefabs[address] = TestHelpers.CreateTestPresenterPrefab<T>();
		}

		public async UniTask<GameObject> InstantiatePrefab(UiConfig config, Transform parent, CancellationToken ct = default)
		{
			InstantiateCallCount++;

			if (ShouldFail)
			{
				throw new Exception($"Simulated load failure for {config.AddressableAddress}");
			}

			if (SimulatedDelayMs > 0)
			{
				await UniTask.Delay(SimulatedDelayMs, cancellationToken: ct);
			}

			if (!_prefabs.TryGetValue(config.AddressableAddress, out var prefab))
			{
				throw new Exception($"Prefab not registered: {config.AddressableAddress}");
			}

			var instance = UnityEngine.Object.Instantiate(prefab, parent);
			_instantiatedObjects.Add(instance);
			return instance;
		}

		public void UnloadAsset(GameObject gameObject)
		{
			UnloadCallCount++;
			if (gameObject != null)
			{
				_instantiatedObjects.Remove(gameObject);
				UnityEngine.Object.DestroyImmediate(gameObject);
			}
		}

		public void Cleanup()
		{
			foreach (var obj in _instantiatedObjects)
			{
				if (obj != null)
				{
					UnityEngine.Object.DestroyImmediate(obj);
				}
			}
			_instantiatedObjects.Clear();
			
			foreach (var prefab in _prefabs.Values)
			{
				if (prefab != null)
				{
					UnityEngine.Object.DestroyImmediate(prefab);
				}
			}
			_prefabs.Clear();
		}
	}

	/// <summary>
	/// Mock analytics for testing
	/// </summary>
	public class MockAnalytics : IUiAnalytics
	{
		public List<(string Event, Type Type)> TrackedEvents { get; } = new();
		public Dictionary<Type, UiPerformanceMetrics> Metrics { get; } = new();

		public UnityEvent<UiEventData> OnUiLoaded { get; } = new();
		public UnityEvent<UiEventData> OnUiOpened { get; } = new();
		public UnityEvent<UiEventData> OnUiClosed { get; } = new();
		public UnityEvent<UiEventData> OnUiUnloaded { get; } = new();
		public UnityEvent<UiPerformanceMetrics> OnPerformanceMetricsUpdated { get; } = new();

		public IReadOnlyDictionary<Type, UiPerformanceMetrics> PerformanceMetrics => Metrics;

		public void TrackLoadStart(Type uiType) => TrackedEvents.Add(("LoadStart", uiType));
		public void TrackLoadComplete(Type uiType, int layer) => TrackedEvents.Add(("LoadComplete", uiType));
		public void TrackOpenStart(Type uiType) => TrackedEvents.Add(("OpenStart", uiType));
		public void TrackOpenComplete(Type uiType, int layer) => TrackedEvents.Add(("OpenComplete", uiType));
		public void TrackCloseStart(Type uiType) => TrackedEvents.Add(("CloseStart", uiType));
		public void TrackCloseComplete(Type uiType, int layer, bool destroyed) => TrackedEvents.Add(("CloseComplete", uiType));
		public void TrackUnload(Type uiType, int layer) => TrackedEvents.Add(("Unload", uiType));

		public UiPerformanceMetrics GetMetrics(Type uiType)
		{
			return Metrics.TryGetValue(uiType, out var metrics) ? metrics : new UiPerformanceMetrics(uiType);
		}

		public void SetCallback(IUiAnalyticsCallback callback) { }
		public void Clear()
		{
			TrackedEvents.Clear();
			Metrics.Clear();
		}
		public void LogPerformanceSummary() { }
	}
}

