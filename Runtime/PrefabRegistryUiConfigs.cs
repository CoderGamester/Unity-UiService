using System;
using System.Collections.Generic;
using UnityEngine;

// ReSharper disable CheckNamespace

namespace GameLovers.UiService
{
	/// <summary>
	/// UiConfigs implementation for PrefabRegistry-based asset loading.
	/// Use this when loading UI presenters via direct prefab references.
	/// </summary>
	[CreateAssetMenu(fileName = "PrefabRegistryUiConfigs", menuName = "ScriptableObjects/Configs/UiConfigs/PrefabRegistry")]
	public class PrefabRegistryUiConfigs : UiConfigs
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

		[SerializeField] private List<PrefabEntry> _prefabEntries = new();

		/// <summary>
		/// Gets the list of prefab entries for use with <see cref="PrefabRegistryUiAssetLoader"/>.
		/// </summary>
		public IReadOnlyList<PrefabEntry> PrefabEntries => _prefabEntries;
	}
}

