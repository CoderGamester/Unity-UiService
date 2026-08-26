using UnityEngine;
using UnityEngine.UIElements;

namespace GameLovers.UiService
{
	/// <summary>Resolves custom and built-in surfaces from presenter hierarchies.</summary>
	internal static class UiSurfaceResolver
	{
		/// <summary>Attempts to resolve the surface that owns a presenter's ordering.</summary>
		internal static bool TryResolve(GameObject presenterObject, out IUiSurface surface)
		{
			// The presenter root owns ordering, so it is exhausted before any descendant is
			// considered. Searching children first lets a nested sub-panel hijack the layer the
			// config authored for the presenter.
			if (TryResolveOn(presenterObject, out surface))
			{
				return true;
			}

			var childSurface = presenterObject.GetComponentInChildren<IUiSurface>(true);
			if (childSurface != null)
			{
				surface = childSurface;
				return true;
			}

			var canvas = presenterObject.GetComponentInChildren<Canvas>(true);
			if (canvas != null)
			{
				surface = new CanvasUiSurface(canvas);
				return true;
			}

			var document = presenterObject.GetComponentInChildren<UIDocument>(true);
			if (document != null)
			{
				surface = new UiDocumentUiSurface(document);
				return true;
			}

			surface = null;
			return false;
		}

		private static bool TryResolveOn(GameObject target, out IUiSurface surface)
		{
			var customSurface = target.GetComponent<IUiSurface>();
			if (customSurface != null)
			{
				surface = customSurface;
				return true;
			}

			if (target.TryGetComponent(out Canvas canvas))
			{
				surface = new CanvasUiSurface(canvas);
				return true;
			}

			if (target.TryGetComponent(out UIDocument document))
			{
				surface = new UiDocumentUiSurface(document);
				return true;
			}

			surface = null;
			return false;
		}
	}

	/// <summary>Adapts a Unity Canvas to the backend-agnostic surface contract.</summary>
	internal sealed class CanvasUiSurface : IUiSurface
	{
		private readonly Canvas _canvas;

		/// <inheritdoc />
		public UiSurfaceSpace SurfaceSpace
		{
			get
			{
				switch (_canvas.renderMode)
				{
					case RenderMode.ScreenSpaceCamera:
						return UiSurfaceSpace.ScreenCamera;
					case RenderMode.WorldSpace:
						return UiSurfaceSpace.World;
					default:
						return UiSurfaceSpace.ScreenOverlay;
				}
			}
		}

		/// <inheritdoc />
		public int Order => _canvas.sortingOrder;

		internal CanvasUiSurface(Canvas canvas)
		{
			_canvas = canvas;
		}

		/// <inheritdoc />
		public void ApplyOrder(int order)
		{
			_canvas.sortingOrder = order;
		}
	}

	/// <summary>Adapts a UI Toolkit document to the backend-agnostic surface contract.</summary>
	internal sealed class UiDocumentUiSurface : IUiSurface
	{
		private readonly UIDocument _document;

		/// <inheritdoc />
		public UiSurfaceSpace SurfaceSpace
		{
			get
			{
				// A document without PanelSettings cannot render at all; report the screen default
				// rather than inventing a world surface the panel will never present.
				var settings = _document.panelSettings;

				return settings != null && settings.renderMode == PanelRenderMode.WorldSpace
					? UiSurfaceSpace.World
					: UiSurfaceSpace.ScreenOverlay;
			}
		}

		/// <inheritdoc />
		public int Order => (int)_document.sortingOrder;

		internal UiDocumentUiSurface(UIDocument document)
		{
			_document = document;
		}

		/// <inheritdoc />
		public void ApplyOrder(int order)
		{
			_document.sortingOrder = order;
		}
	}
}
