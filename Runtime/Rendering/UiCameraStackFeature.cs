using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GameLovers.UiService.Rendering
{
	/// <summary>
	/// Lets a presenter's <see cref="Canvas"/> render as Screen Space - Camera and stack, via
	/// URP camera stacking, onto a base camera -- so UI can layer over 3D content with correct
	/// sorting instead of always compositing on top of everything as Screen Space - Overlay does.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Every sample canvas in this package ships as Screen Space - Overlay, so this feature is
	/// net-new capability, not a migration of existing behaviour -- opting a presenter in changes
	/// its render mode.
	/// </para>
	/// <para>
	/// <b>Screen Space - Overlay and Screen Space - Camera do not share one sort order.</b>
	/// Overlay canvases composite after ALL URP rendering, straight to the backbuffer. A stacked
	/// Screen Space - Camera presenter at a high <c>UiConfig.Layer</c> still draws underneath an
	/// Overlay presenter at layer 0 the moment both exist in the same scene -- <c>UiConfig.Layer</c>
	/// stops being a total order across render modes. This is a fundamental property of how the
	/// two composite, not a bug, and there is no runtime fix; keep Overlay and Camera-stacked
	/// presenters visually separated (e.g. HUD as Overlay, world-anchored UI as stacked) or accept
	/// the fixed relative order.
	/// </para>
	/// </remarks>
	public class UiCameraStackFeature : PresenterFeatureBase
	{
		[SerializeField] private Camera _overlayCamera;
		[SerializeField] private Canvas _canvas;
		[SerializeField] private float _planeDistance = 100f;
		[SerializeField] private bool _useCanvasSortingOrder = true;
		[SerializeField] private int _stackPriority;

		private IUiBaseCameraProvider _baseCameraProvider = UiBaseCameraProvider.Default;
		private bool _isStacked;

		/// <summary>True while this presenter's overlay camera is inserted in the base camera's stack.</summary>
		public bool IsStacked => _isStacked;

		/// <summary>
		/// Swaps the base-camera resolution strategy (default: <see cref="UiBaseCameraProvider.Default"/>,
		/// i.e. <see cref="Camera.main"/>). Call before the presenter opens.
		/// </summary>
		public void SetBaseCameraProvider(IUiBaseCameraProvider provider)
		{
			_baseCameraProvider = provider ?? UiBaseCameraProvider.Default;
		}

		/// <summary>Test-only configuration hook (package tests have InternalsVisibleTo access).</summary>
		internal void ConfigureForTest(Camera overlayCamera, Canvas canvas, IUiBaseCameraProvider provider = null,
			bool useCanvasSortingOrder = true, int stackPriority = 0)
		{
			_overlayCamera = overlayCamera;
			_canvas = canvas;
			_useCanvasSortingOrder = useCanvasSortingOrder;
			_stackPriority = stackPriority;
			_baseCameraProvider = provider ?? UiBaseCameraProvider.Default;
		}

		/// <inheritdoc />
		public override void OnPresenterInitialized(UiPresenter presenter)
		{
			base.OnPresenterInitialized(presenter);

			if (_overlayCamera == null || _canvas == null)
			{
				Debug.LogError($"{nameof(UiCameraStackFeature)} on '{name}' requires both an overlay Camera" +
					" and a Canvas to be assigned.", this);
				return;
			}

			_canvas.renderMode = RenderMode.ScreenSpaceCamera;
			_canvas.worldCamera = _overlayCamera;
			_canvas.planeDistance = _planeDistance;

			var overlayData = _overlayCamera.GetUniversalAdditionalCameraData();
			overlayData.renderType = CameraRenderType.Overlay;
			overlayData.renderPostProcessing = false;
			// clearDepth has no public/internal setter in this URP version (get-only, "only
			// valid for Overlay cameras" per its own doc comment) -- its true default is the
			// correct behaviour for a UI overlay camera anyway, so there is nothing to configure.

			// Driven entirely by the stack; URP ignores a standalone-enabled Overlay camera's
			// own render, but disabling avoids relying on that and matches how Overlay cameras
			// are normally authored (enabled only once actually stacked).
			_overlayCamera.enabled = false;
		}

		/// <inheritdoc />
		public override void OnPresenterOpening()
		{
			base.OnPresenterOpening();
			InsertIntoStack();
		}

		private void InsertIntoStack()
		{
			if (_isStacked || _overlayCamera == null || _canvas == null)
			{
				return;
			}

			var baseCamera = _baseCameraProvider?.GetBaseCamera();
			if (baseCamera == null || baseCamera == _overlayCamera)
			{
				return;
			}

			var baseData = baseCamera.GetUniversalAdditionalCameraData();
			var stack = baseData.cameraStack;

			if (stack.Contains(_overlayCamera))
			{
				_isStacked = true;
				return;
			}

			var priority = _useCanvasSortingOrder ? _canvas.sortingOrder : _stackPriority;
			var priorities = new List<int>(stack.Count);
			foreach (var camera in stack)
			{
				var marker = camera != null ? camera.GetComponent<UiCameraStackPriorityMarker>() : null;
				priorities.Add(marker != null ? marker.Priority : 0);
			}

			var index = UiCameraStackRegistry.InsertIndex(priorities, priority);
			stack.Insert(index, _overlayCamera);

			if (!_overlayCamera.TryGetComponent<UiCameraStackPriorityMarker>(out var ownMarker))
			{
				ownMarker = _overlayCamera.gameObject.AddComponent<UiCameraStackPriorityMarker>();
			}
			ownMarker.Priority = priority;

			_isStacked = true;
		}

		private void OnDisable()
		{
			RemoveFromStack();
		}

		private void RemoveFromStack()
		{
			if (!_isStacked || _overlayCamera == null)
			{
				_isStacked = false;
				return;
			}

			var baseCamera = _baseCameraProvider?.GetBaseCamera();
			if (baseCamera != null)
			{
				var baseData = baseCamera.GetUniversalAdditionalCameraData();
				baseData.cameraStack.Remove(_overlayCamera);
			}

			_isStacked = false;
		}
	}
}
