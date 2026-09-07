using UnityEngine;
using UnityEngine.UIElements;

// ReSharper disable CheckNamespace

namespace GameLovers.UiService.Views
{
	/// <summary>
	/// Insets a full-panel UI Toolkit content root so its children clear the device safe area.
	/// </summary>
	/// <remarks>
	/// Keep full-screen backgrounds outside this element. Put absolute-positioned controls in a relatively positioned, growing child so they use the inset content bounds.
	/// </remarks>
	[UxmlElement]
	public partial class SafeAreaContainer : VisualElement
	{
		private const long UpdateIntervalMilliseconds = 100;

		private IVisualElementScheduledItem _scheduledUpdate;
		private Vector4 _appliedInsets;
		private bool _hasAppliedInsets;

		public SafeAreaContainer()
		{
			style.flexGrow = 1f;

			RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
			RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanel);
			RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
		}

		/// <summary>
		/// Converts a pixel safe area into UI Toolkit padding in panel points.
		/// </summary>
		internal static void ComputeInsets(Rect safeArea, int width, int height, float scaledPixelsPerPoint, out float left, out float top, out float right, out float bottom)
		{
			if (width <= 0 || height <= 0 || scaledPixelsPerPoint <= 0f)
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

		/// <summary>
		/// Recalculates and applies the current safe-area padding.
		/// </summary>
		internal void Refresh()
		{
			if (!Application.isPlaying || panel == null || panel.contextType == ContextType.Editor)
			{
				ApplyInsets(0f, 0f, 0f, 0f);
				return;
			}

			ApplySafeArea(UnityEngine.Device.Screen.safeArea, UnityEngine.Device.Screen.width, UnityEngine.Device.Screen.height);
		}

		/// <summary>
		/// Applies a pixel safe area using the attached panel scale, or one when detached.
		/// </summary>
		internal void ApplySafeArea(Rect safeArea, int width, int height)
		{
			float scaledPixelsPerPoint = panel?.scaledPixelsPerPoint ?? 1f;
			ComputeInsets(safeArea, width, height, scaledPixelsPerPoint, out float left, out float top, out float right, out float bottom);
			ApplyInsets(left, top, right, bottom);
		}

		private void OnAttachToPanel(AttachToPanelEvent evt)
		{
			_hasAppliedInsets = false;
			Refresh();
			if (!Application.isPlaying || panel == null || panel.contextType == ContextType.Editor)
			{
				return;
			}

			if (_scheduledUpdate == null)
			{
				_scheduledUpdate = schedule.Execute(Refresh).Every(UpdateIntervalMilliseconds);
				return;
			}

			_scheduledUpdate.Resume();
		}

		private void OnDetachFromPanel(DetachFromPanelEvent evt)
		{
			_scheduledUpdate?.Pause();
			_hasAppliedInsets = false;
		}

		private void OnGeometryChanged(GeometryChangedEvent evt)
		{
			Refresh();
		}

		private void ApplyInsets(float left, float top, float right, float bottom)
		{
			var insets = new Vector4(left, top, right, bottom);
			if (_hasAppliedInsets && _appliedInsets == insets)
			{
				return;
			}

			_hasAppliedInsets = true;
			_appliedInsets = insets;
			style.paddingLeft = left;
			style.paddingTop = top;
			style.paddingRight = right;
			style.paddingBottom = bottom;
		}
	}
}
