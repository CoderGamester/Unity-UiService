using UnityEngine;

// ReSharper disable CheckNamespace

namespace GameLovers.UiService.Views
{
	/// <summary>
	/// Maps a stretched uGUI container to the device safe area so children clear screen obstructions.
	/// </summary>
	/// <remarks>
	/// Place this component on a full-parent stretched rect and keep full-screen backgrounds outside it.
	/// </remarks>
	[DisallowMultipleComponent]
	[RequireComponent(typeof(RectTransform))]
	public sealed class SafeAreaPanelView : MonoBehaviour
	{
		private RectTransform _rect;
		private Rect _appliedSafeArea;
		private int _appliedWidth;
		private int _appliedHeight;
		private bool _hasAppliedSafeArea;

		private void Awake()
		{
			_rect = (RectTransform)transform;
		}

		private void OnEnable()
		{
			_hasAppliedSafeArea = false;
			Apply();
		}

		private void Update()
		{
			Apply();
		}

		private void OnRectTransformDimensionsChange()
		{
			Apply();
		}

		/// <summary>
		/// Converts a pixel safe-area rectangle into normalized <see cref="RectTransform"/> anchors.
		/// </summary>
		internal static void ComputeNormalizedAnchors(Rect safeArea, int width, int height, out Vector2 anchorMin, out Vector2 anchorMax)
		{
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
		/// Applies the current device safe area to this rect in Play Mode.
		/// </summary>
		internal void Apply()
		{
			if (!Application.isPlaying || !isActiveAndEnabled)
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
			if (_hasAppliedSafeArea && !HasScreenChanged(safeArea, width, height, _appliedSafeArea, _appliedWidth, _appliedHeight))
			{
				return;
			}

			_hasAppliedSafeArea = true;
			_appliedSafeArea = safeArea;
			_appliedWidth = width;
			_appliedHeight = height;

			ComputeNormalizedAnchors(safeArea, width, height, out Vector2 anchorMin, out Vector2 anchorMax);
			_rect = _rect != null ? _rect : (RectTransform)transform;
			_rect.anchorMin = anchorMin;
			_rect.anchorMax = anchorMax;
			_rect.offsetMin = Vector2.zero;
			_rect.offsetMax = Vector2.zero;
		}
	}
}
