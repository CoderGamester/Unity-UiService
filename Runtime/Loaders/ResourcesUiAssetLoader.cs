using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace GameLovers.UiService
{
	/// <summary>
	/// Implementation of <see cref="IUiAssetLoader"/> that loads assets from the Resources folder.
	/// </summary>
	public class ResourcesUiAssetLoader : IUiAssetLoader
	{
		private readonly Dictionary<string, GameObject> _cachedPrefabs = new();

		/// <inheritdoc />
		public UniTask<GameObject> InstantiatePrefab(UiConfig config, Transform parent, CancellationToken cancellationToken = default)
		{
			if (!_cachedPrefabs.TryGetValue(config.AddressableAddress, out var prefab))
			{
				prefab = Resources.Load<GameObject>(config.AddressableAddress);
				
				if (prefab == null)
				{
					throw new KeyNotFoundException($"Prefab not found in Resources at path: {config.AddressableAddress}");
				}
				
				_cachedPrefabs[config.AddressableAddress] = prefab;
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
				Object.DestroyImmediate(asset);
			}
		}
	}
}

