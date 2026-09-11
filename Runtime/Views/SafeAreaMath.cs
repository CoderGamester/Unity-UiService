using UnityEngine;

// ReSharper disable CheckNamespace

namespace GameLovers.UiService.Views
{
	/// <summary>
	/// Single owner of the safe-area math shared by the uGUI and UI Toolkit containers.
	/// </summary>
	internal static class SafeAreaMath
	{
		/// <summary>
		/// Reports whether a measured extent can participate in safe-area math.
		/// </summary>
		internal static bool IsMeasurable(float value)
		{
			return IsFinite(value) && value > 0f;
		}

		/// <summary>
		/// Converts a pixel safe-area rectangle into normalized <see cref="RectTransform"/> anchors.
		/// </summary>
		internal static void ComputeNormalizedAnchors(Rect safeArea, int width, int height, out Vector2 anchorMin, out Vector2 anchorMax)
		{
			if (!IsMeasurable(width) || !IsMeasurable(height) || !IsFinite(safeArea))
			{
				anchorMin = Vector2.zero;
				anchorMax = Vector2.one;
				return;
			}

			anchorMin = new Vector2(safeArea.xMin / width, safeArea.yMin / height);
			anchorMax = new Vector2(safeArea.xMax / width, safeArea.yMax / height);
		}

		/// <summary>
		/// Reports whether the device safe area or its pixel bounds changed since the previous application.
		/// </summary>
		internal static bool HasScreenChanged(Rect safeArea, int width, int height, Rect appliedSafeArea, int appliedWidth, int appliedHeight)
		{
			return safeArea != appliedSafeArea || width != appliedWidth || height != appliedHeight;
		}

		/// <summary>
		/// Converts a pixel safe area into UI Toolkit padding in panel points.
		/// </summary>
		internal static void ComputeInsets(Rect safeArea, int width, int height, float scaledPixelsPerPoint, out float left, out float top, out float right, out float bottom)
		{
			if (!IsMeasurable(width) || !IsMeasurable(height) || !IsMeasurable(scaledPixelsPerPoint) || !IsFinite(safeArea))
			{
				left = 0f;
				top = 0f;
				right = 0f;
				bottom = 0f;
				return;
			}

			left = Mathf.Max(0f, safeArea.xMin) / scaledPixelsPerPoint;
			top = Mathf.Max(0f, height - safeArea.yMax) / scaledPixelsPerPoint;
			right = Mathf.Max(0f, width - safeArea.xMax) / scaledPixelsPerPoint;
			bottom = Mathf.Max(0f, safeArea.yMin) / scaledPixelsPerPoint;
		}

		private static bool IsFinite(float value)
		{
			return !float.IsNaN(value) && !float.IsInfinity(value);
		}

		private static bool IsFinite(Rect rect)
		{
			return IsFinite(rect.x) && IsFinite(rect.y) && IsFinite(rect.width) && IsFinite(rect.height);
		}
	}
}
