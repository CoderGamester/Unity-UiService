using System.Collections.Generic;
using UnityEngine;

namespace GameLovers.UiService.Rendering
{
	/// <summary>
	/// Refcounted registry of active backdrop-blur requests. Deliberately has zero URP/engine-
	/// rendering references (only <see cref="UnityEngine.Object"/> for the owner key and
	/// <see cref="UnityEngine.Color"/>/<see cref="Mathf"/> inside <see cref="UiBackdropBlurSettings"/>),
	/// so its refcounting and winner-resolution logic stay testable without a URP reference.
	/// </summary>
	/// <remarks>
	/// A <see cref="UnityEngine.Rendering.ScriptableRendererFeature"/> is a single shared asset
	/// instance across all cameras, so this registry is intentionally process-global rather than
	/// per-presenter. It resets on every play-mode entry via <see cref="ResetOnLoad"/> --
	/// with Domain Reload disabled, static fields survive between play sessions, so without this
	/// reset a second play session would start with stale requests from the first.
	/// </remarks>
	public static class UiBackdropBlurRequests
	{
		private static readonly Dictionary<object, (UiBackdropBlurSettings settings, int sortKey)> _requests = new();

		/// <summary>Registers or updates a request. <paramref name="owner"/> is any stable per-request identity (typically the requesting feature instance).</summary>
		public static void Register(object owner, UiBackdropBlurSettings settings, int sortKey)
		{
			if (owner == null)
			{
				return;
			}

			_requests[owner] = (settings, sortKey);
		}

		/// <summary>Removes a previously registered request. No-op if not registered.</summary>
		public static void Unregister(object owner)
		{
			if (owner == null)
			{
				return;
			}

			_requests.Remove(owner);
		}

		/// <summary>Number of currently active requests.</summary>
		public static int ActiveCount => _requests.Count;

		/// <summary>
		/// Resolves the winning request: highest <c>sortKey</c> (topmost), first-registered wins ties.
		/// Returns false if there are no active requests.
		/// </summary>
		public static bool TryGetActive(out UiBackdropBlurSettings settings)
		{
			var found = false;
			var bestKey = 0;
			var best = default(UiBackdropBlurSettings);

			foreach (var entry in _requests)
			{
				if (!found || entry.Value.sortKey > bestKey)
				{
					found = true;
					bestKey = entry.Value.sortKey;
					best = entry.Value.settings;
				}
			}

			settings = best;
			return found;
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetOnLoad()
		{
			_requests.Clear();
		}
	}
}
