using UnityEngine;

namespace GameLoversEditor.UiService
{
	/// <summary>Reads and writes presenter order through the runtime surface contract.</summary>
	internal static class UiSurfaceEditorUtility
	{
		/// <summary>Returns the resolved surface order, or -1 when the prefab has no surface.</summary>
		internal static int GetOrder(GameLovers.UiService.UiPresenter presenter)
		{
			return GameLovers.UiService.UiSurfaceResolver.TryResolve(presenter.gameObject, out var surface)
				? surface.Order
				: -1;
		}

		/// <summary>Returns the resolved render space, defaulting to screen overlay when no surface exists.</summary>
		internal static GameLovers.UiService.UiSurfaceSpace GetSpace(GameLovers.UiService.UiPresenter presenter)
		{
			return GameLovers.UiService.UiSurfaceResolver.TryResolve(presenter.gameObject, out var surface)
				? surface.SurfaceSpace
				: GameLovers.UiService.UiSurfaceSpace.ScreenOverlay;
		}

		/// <summary>Applies an order through the resolved surface and reports whether one was found.</summary>
		internal static bool ApplyOrder(GameObject presenterObject, int order)
		{
			if (!GameLovers.UiService.UiSurfaceResolver.TryResolve(presenterObject, out var surface))
			{
				return false;
			}

			surface.ApplyOrder(order);
			return true;
		}
	}
}
