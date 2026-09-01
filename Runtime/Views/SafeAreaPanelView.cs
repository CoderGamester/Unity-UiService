using UnityEngine;

// ReSharper disable CheckNamespace

namespace GameLovers.UiService.Views
{
	/// <summary>
	/// Maps a stretched uGUI container to the device safe area so children clear notches without per-widget insets.
	/// </summary>
	/// <remarks>
	/// Uses <see cref="UnityEngine.Device.Screen"/> so the Device Simulator and the player report the same inset.
	/// Skips Edit Mode because <c>OnRectTransformDimensionsChange</c> can fire while the Game View disagrees with the simulated safe area.
	/// Stretch a child under the canvas, add this component, and parent HUD chrome to that child. For a single edge-anchored widget, use <see cref="SafeAreaHelperView"/>.
	/// </remarks>
	[DisallowMultipleComponent]
	[RequireComponent(typeof(RectTransform))]
	public sealed class SafeAreaPanelView : MonoBehaviour
	{
		private RectTransform _rect;
		private Rect _appliedSafeArea;
		private int _appliedWidth;
		private int _appliedHeight;

		private void Awake()
		{
			_rect = (RectTransform)transform;
			Apply();
		}

		private void OnEnable()
		{
			Apply();
		}

		private void OnRectTransformDimensionsChange()
		{
			Apply();
		}

		/// <summary>Converts a pixel safe-area rectangle into normalized <see cref="RectTransform"/> anchors.</summary>
		internal static void ComputeNormalizedAnchors(Rect safeArea, int width, int height, out Vector2 anchorMin, out Vector2 anchorMax)
		{
			anchorMin = new Vector2(safeArea.xMin / width, safeArea.yMin / height);
			anchorMax = new Vector2(safeArea.xMax / width, safeArea.yMax / height);
		}

		/// <summary>Applies the current device safe area to this rect in Play Mode.</summary>
		internal void Apply()
		{
			if (_rect == null)
			{
				_rect = (RectTransform)transform;
			}

			if (!Application.isPlaying)
			{
				return;
			}

			int width = UnityEngine.Device.Screen.width;
			int height = UnityEngine.Device.Screen.height;
			if (width <= 0 || height <= 0)
			{
				return;
			}

			Rect safeArea = UnityEngine.Device.Screen.safeArea;
			if (safeArea == _appliedSafeArea && width == _appliedWidth && height == _appliedHeight)
			{
				return;
			}

			_appliedSafeArea = safeArea;
			_appliedWidth = width;
			_appliedHeight = height;

			ComputeNormalizedAnchors(safeArea, width, height, out Vector2 anchorMin, out Vector2 anchorMax);
			_rect.anchorMin = anchorMin;
			_rect.anchorMax = anchorMax;
			_rect.offsetMin = Vector2.zero;
			_rect.offsetMax = Vector2.zero;
		}
	}
}
