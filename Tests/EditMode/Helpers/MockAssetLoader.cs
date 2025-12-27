using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameLovers.UiService.Tests
{
	/// <summary>
	/// Mock asset loader for testing without Addressables.
	/// This is a Fake (not a Mock) because it has real behavior - it actually instantiates GameObjects.
	/// NSubstitute cannot be used here because we need real Unity object lifecycle management.
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
}

