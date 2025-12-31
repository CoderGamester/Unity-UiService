using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace GameLovers.UiService
{
    /// <summary>
    /// ScriptableObject that stores (address, prefab) pairs for use with <see cref="PrefabRegistryUiAssetLoader"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "PrefabRegistryConfig", menuName = "ScriptableObjects/Configs/PrefabRegistryConfig")]
    public class PrefabRegistryConfig : ScriptableObject
    {
        /// <summary>
        /// Represents an entry mapping an address to a prefab.
        /// </summary>
        [Serializable]
        public struct PrefabEntry
        {
            public string Address;
            public GameObject Prefab;
        }

        [SerializeField] private List<PrefabEntry> _entries = new();

        /// <summary>
        /// Gets the list of prefab entries.
        /// </summary>
        public IReadOnlyList<PrefabEntry> Entries => _entries;
    }
}

