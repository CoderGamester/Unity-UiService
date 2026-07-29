using System.Collections.Generic;
using UnityEngine;

namespace GameLovers.UiService.Rendering
{
	/// <summary>
	/// Registry of the backdrop-blur requests currently held open by presenters, and the decision of
	/// whether a given camera should render one.
	/// </summary>
	/// <remarks>
	/// Process-global because a <see cref="UnityEngine.Rendering.ScriptableRendererFeature"/> is one
	/// shared asset instance across every camera, with no reference path back to a presenter prefab.
	/// Holds no URP types, so the resolution and injection rules stay testable in EditMode.
	/// </remarks>
	public static class UiBackdropBlurRequests
	{
		private static readonly Dictionary<object, (UiBackdropBlurSettings settings, int sortKey)> _requests = new();

		/// <summary>Number of currently active requests.</summary>
		public static int ActiveCount => _requests.Count;

		/// <summary>
		/// Whether the blur pass should run against a camera with the given traits.
		/// </summary>
		/// <remarks>
		/// Scene and Preview cameras would blur the Editor's own views; a URP overlay camera would blur
		/// the stacked UI rather than the world behind it; a render-texture camera would blur the UI
		/// being captured.
		/// </remarks>
		public static bool ShouldInject(bool isGameCamera, bool isOverlayCamera, bool hasTargetTexture, bool anyActiveRequests)
		{
			return isGameCamera && !isOverlayCamera && !hasTargetTexture && anyActiveRequests;
		}

		/// <summary>
		/// Registers or updates the request owned by <paramref name="owner"/>.
		/// </summary>
		public static void Register(object owner, UiBackdropBlurSettings settings, int sortKey)
		{
			if (owner == null)
			{
				return;
			}

			_requests[owner] = (settings, sortKey);
		}

		/// <summary>
		/// Removes a previously registered request, or does nothing if it was never registered.
		/// </summary>
		public static void Unregister(object owner)
		{
			if (owner == null)
			{
				return;
			}

			_requests.Remove(owner);
		}

		/// <summary>
		/// Resolves the winning request: highest sort key, first-registered breaking ties.
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

		// Statics survive between play sessions when Domain Reload is disabled.
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetOnLoad()
		{
			_requests.Clear();
		}
	}
}
