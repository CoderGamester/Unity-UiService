using Rive.Components;
using UnityEngine;

namespace GameLovers.UiService.Rive
{
	/// <summary>
	/// Adapts a Rive panel renderer to UiService ordering and render-space contracts.
	/// </summary>
	public sealed class RiveUiSurface : MonoBehaviour, IUiSurface
	{
		[SerializeField]
		private PanelRenderer _panelRenderer;
		[SerializeField]
		private UiSurfaceSpace _surfaceSpace;

		/// <summary>Gets or assigns the Rive component that displays this surface.</summary>
		public PanelRenderer PanelRenderer
		{
			get => _panelRenderer;
			set => _panelRenderer = value;
		}

		/// <inheritdoc />
		public UiSurfaceSpace SurfaceSpace
		{
			get => _surfaceSpace;
			set => _surfaceSpace = value;
		}

		/// <inheritdoc />
		public int Order
		{
			get
			{
				if (_panelRenderer is RiveCanvasRenderer canvasRenderer)
				{
					return canvasRenderer.Canvas.sortingOrder;
				}

				if (_panelRenderer is RiveTextureRenderer textureRenderer &&
					textureRenderer.Renderer != null)
				{
					return textureRenderer.Renderer.sortingOrder;
				}

				return 0;
			}
		}

		/// <inheritdoc />
		public void ApplyOrder(int order)
		{
			if (_panelRenderer is RiveCanvasRenderer canvasRenderer)
			{
				canvasRenderer.Canvas.sortingOrder = order;
				return;
			}

			if (_panelRenderer is RiveTextureRenderer textureRenderer &&
				textureRenderer.Renderer != null)
			{
				textureRenderer.Renderer.sortingOrder = order;
				return;
			}

			Debug.LogWarning(
				$"{nameof(RiveUiSurface)} on '{name}' has no supported panel renderer, so order {order} was not applied.",
				this);
		}
	}
}
