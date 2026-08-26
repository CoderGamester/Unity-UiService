using System;
using System.Collections.Generic;
using Rive.Components;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameLovers.UiService.Rive
{
	/// <summary>
	/// Owns a reference-counted render-target strategy shared by Rive panels.
	/// </summary>
	public sealed class RiveRenderTargetLease : IDisposable
	{
		private readonly UiRenderTargetPolicy _policy;
		private readonly string _groupId;
		private bool _disposed;

		/// <summary>Gets the strategy assigned to panels in this lease.</summary>
		public IRenderTargetStrategy Strategy { get; }

		internal RiveRenderTargetLease(
			UiRenderTargetPolicy policy,
			string groupId,
			IRenderTargetStrategy strategy)
		{
			_policy = policy;
			_groupId = groupId;
			Strategy = strategy;
		}

		/// <summary>
		/// Releases this consumer and destroys the shared strategy after its last lease.
		/// </summary>
		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			RiveRenderTargetRegistry.Release(_policy, _groupId);
		}
	}

	/// <summary>Coordinates reference-counted Rive render-target strategy groups.</summary>
	internal static class RiveRenderTargetRegistry
	{
		private static readonly Dictionary<Key, Entry> Entries = new Dictionary<Key, Entry>();

		private static GameObject _root;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Reset()
		{
			Entries.Clear();
			_root = null;
		}

		/// <summary>Acquires a strategy lease for a policy and sharing group.</summary>
		internal static RiveRenderTargetLease Acquire(
			UiRenderTargetPolicy policy,
			string groupId,
			Vector2Int textureSize,
			int initialPoolSize,
			int maxPoolSize)
		{
			if (policy == UiRenderTargetPolicy.Dedicated)
			{
				return new RiveRenderTargetLease(policy, string.Empty, null);
			}

			if (string.IsNullOrWhiteSpace(groupId))
			{
				throw new ArgumentException(
					"A shared Rive render-target policy requires a non-empty group id.",
					nameof(groupId));
			}

			var key = new Key(policy, groupId);
			if (!Entries.TryGetValue(key, out var entry))
			{
				entry = CreateEntry(key, textureSize, initialPoolSize, maxPoolSize);
				Entries.Add(key, entry);
			}

			entry.ReferenceCount++;
			return new RiveRenderTargetLease(policy, groupId, entry.Strategy);
		}

		/// <summary>Releases one group consumer and destroys an unused strategy.</summary>
		internal static void Release(UiRenderTargetPolicy policy, string groupId)
		{
			if (policy == UiRenderTargetPolicy.Dedicated)
			{
				return;
			}

			var key = new Key(policy, groupId);
			if (!Entries.TryGetValue(key, out var entry))
			{
				return;
			}

			entry.ReferenceCount--;
			if (entry.ReferenceCount > 0)
			{
				return;
			}

			Entries.Remove(key);
			Destroy(entry.Owner);
			if (Entries.Count == 0)
			{
				Destroy(_root);
				_root = null;
			}
		}

		private static Entry CreateEntry(
			Key key,
			Vector2Int textureSize,
			int initialPoolSize,
			int maxPoolSize)
		{
			if (_root == null)
			{
				_root = new GameObject("[UiService] Rive Render Targets");
				if (Application.isPlaying)
				{
					Object.DontDestroyOnLoad(_root);
				}
			}

			var owner = new GameObject($"{key.Policy} - {key.GroupId}");
			owner.transform.SetParent(_root.transform, false);

			IRenderTargetStrategy strategy;
			if (key.Policy == UiRenderTargetPolicy.SharedAtlas)
			{
				var atlas = owner.AddComponent<AtlasRenderTargetStrategy>();
				atlas.Configure(textureSize, textureSize, Mathf.Max(textureSize.x, textureSize.y), 2);
				strategy = atlas;
			}
			else
			{
				var pool = owner.AddComponent<PooledRenderTargetStrategy>();
				pool.Configure(
					textureSize,
					Mathf.Max(1, initialPoolSize),
					Mathf.Max(initialPoolSize, maxPoolSize),
					PooledRenderTargetStrategy.PoolOverflowBehavior.Flexible);
				strategy = pool;
			}

			return new Entry
			{
				Owner = owner,
				Strategy = strategy,
				ReferenceCount = 0
			};
		}

		private static void Destroy(Object target)
		{
			if (target == null)
			{
				return;
			}

			if (Application.isPlaying)
			{
				Object.Destroy(target);
				return;
			}

			Object.DestroyImmediate(target);
		}

		private sealed class Entry
		{
			public GameObject Owner;
			public IRenderTargetStrategy Strategy;
			public int ReferenceCount;
		}

		private readonly struct Key : IEquatable<Key>
		{
			public readonly UiRenderTargetPolicy Policy;
			public readonly string GroupId;

			public Key(UiRenderTargetPolicy policy, string groupId)
			{
				Policy = policy;
				GroupId = groupId;
			}

			public bool Equals(Key other)
			{
				return Policy == other.Policy && GroupId == other.GroupId;
			}

			public override bool Equals(object obj)
			{
				return obj is Key other && Equals(other);
			}

			public override int GetHashCode()
			{
				return HashCode.Combine((int)Policy, GroupId);
			}
		}
	}
}
