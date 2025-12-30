using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace GameLovers.UiService
{
	/// <summary>
	/// Implementation of <see cref="IUiAssetLoader"/> that uses a dictionary to map addresses to prefabs.
	/// This is useful for testing or when prefabs are directly referenced in a ScriptableObject or MonoBehavior.
	/// </summary>
	public class PrefabRegistryUiAssetLoader : IUiAssetLoader
	{
		private readonly Dictionary<string, GameObject> _prefabMap = new();

		/// <summary>
		/// Registers a prefab with a given address.
		/// </summary>
		public void RegisterPrefab(string address, GameObject prefab)
		{
			_prefabMap[address] = prefab;
		}

		/// <inheritdoc />
		public UniTask<GameObject> InstantiatePrefab(UiConfig config, Transform parent, CancellationToken cancellationToken = default)
		{
			if (!_prefabMap.TryGetValue(config.AddressableAddress, out var prefab))
			{
				throw new KeyNotFoundException($"Prefab not registered for address: {config.AddressableAddress}");
			}

			var instance = Object.Instantiate(prefab, parent);
			instance.SetActive(false);
			
			return UniTask.FromResult(instance);
		}

		/// <inheritdoc />
		public void UnloadAsset(GameObject asset)
		{
			if (asset != null)
			{
				Object.Destroy(asset);
			}
		}
	}
}

