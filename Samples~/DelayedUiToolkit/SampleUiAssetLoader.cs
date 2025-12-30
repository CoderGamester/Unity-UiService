using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace GameLovers.UiService.Examples
{
    /// <summary>
    /// Simple asset loader for samples that uses direct prefab references.
    /// Avoids Addressables dependency for quick sample testing.
    /// </summary>
    public class SampleUiAssetLoader : IUiAssetLoader
    {
        private readonly Dictionary<string, GameObject> _prefabMap = new();

        public void RegisterPrefab(string address, GameObject prefab)
        {
            _prefabMap[address] = prefab;
        }

        public UniTask<GameObject> InstantiatePrefab(UiConfig config, Transform parent, CancellationToken ct = default)
        {
            if (!_prefabMap.TryGetValue(config.AddressableAddress, out var prefab))
            {
                throw new KeyNotFoundException($"Prefab not registered for address: {config.AddressableAddress}");
            }

            var instance = Object.Instantiate(prefab, parent);
            instance.SetActive(false);
            return UniTask.FromResult(instance);
        }

        public void UnloadAsset(GameObject asset)
        {
            if (asset != null)
            {
                Object.Destroy(asset);
            }
        }
    }
}

