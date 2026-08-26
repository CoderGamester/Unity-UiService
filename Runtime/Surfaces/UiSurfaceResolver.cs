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
			var behaviours = presenterObject.GetComponentsInChildren<MonoBehaviour>(true);
			foreach (var behaviour in behaviours)
			{
				if (behaviour is IUiSurface customSurface)
				{
					surface = customSurface;
					return true;
				}
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
		public UiSurfaceSpace SurfaceSpace => UiSurfaceSpace.ScreenOverlay;

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
