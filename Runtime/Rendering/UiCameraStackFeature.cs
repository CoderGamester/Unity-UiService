using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace GameLovers.UiService.Rendering
{
	/// <summary>
	/// Renders a presenter's <see cref="Canvas"/> as Screen Space - Camera and stacks its overlay
	/// camera onto a base camera, so the UI can layer over 3D content with correct sorting.
	/// </summary>
	/// <remarks>
	/// Opting a presenter in changes its render mode, and Screen Space - Overlay and Screen Space -
	/// Camera do not share one sort order: Overlay composites after all URP rendering, so a stacked
	/// presenter at a high <c>UiConfig.Layer</c> still draws underneath an Overlay presenter at layer
	/// 0. Keep the two groups visually separated, or accept the fixed relative order.
	/// </remarks>
	public class UiCameraStackFeature : PresenterFeatureBase
	{
		// URP's cameraStack is a plain List<Camera> with no priority of its own. Keyed by Camera
		// rather than derived from the hierarchy because the serialized camera need not be a child of
		// the presenter, which would silently sort an externally-assigned camera as priority 0.
		private static readonly Dictionary<Camera, int> _stackPriorities = new();

		[SerializeField] private Camera _overlayCamera;
		[SerializeField] private Canvas _canvas;
		[SerializeField] private float _planeDistance = 100f;
		[SerializeField] private bool _useCanvasSortingOrder = true;
		[SerializeField] private int _stackPriority;

		private Func<Camera> _baseCameraResolver = ResolveMainCamera;
		private bool _isStacked;

		/// <summary>True while this presenter's overlay camera is inserted in the base camera's stack.</summary>
		public bool IsStacked => _isStacked;

		/// <summary>The priority this presenter's overlay camera is ordered by within the stack.</summary>
		public int StackPriority => _useCanvasSortingOrder && _canvas != null ? _canvas.sortingOrder : _stackPriority;

		/// <summary>Resolves the base camera to stack onto. Assign before the presenter opens; null restores <see cref="Camera.main"/>.</summary>
		public Func<Camera> BaseCameraResolver
		{
			get => _baseCameraResolver;
			set => _baseCameraResolver = value ?? ResolveMainCamera;
		}

		private void OnDisable()
		{
			RemoveFromStack();
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

			// URP drives stacked cameras itself; clearDepth has no setter and already defaults correctly.
			_overlayCamera.enabled = false;
		}

		/// <inheritdoc />
		public override void OnPresenterOpening()
		{
			base.OnPresenterOpening();
			InsertIntoStack();
		}

		internal void ConfigureForTest(Camera overlayCamera, Canvas canvas, Func<Camera> baseCameraResolver = null,
			bool useCanvasSortingOrder = true, int stackPriority = 0)
		{
			_overlayCamera = overlayCamera;
			_canvas = canvas;
			_useCanvasSortingOrder = useCanvasSortingOrder;
			_stackPriority = stackPriority;
			_baseCameraResolver = baseCameraResolver ?? ResolveMainCamera;
		}

		// Camera.main is a cached tag lookup resolved twice per open/close, so it needs no caching of
		// its own; an earlier caching provider saved nothing and leaked a static sceneLoaded handler.
		private static Camera ResolveMainCamera() => Camera.main;

		// Statics survive between play sessions when Domain Reload is disabled.
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetOnLoad()
		{
			_stackPriorities.Clear();
		}

		private void InsertIntoStack()
		{
			if (_isStacked || _overlayCamera == null || _canvas == null)
			{
				return;
			}

			var baseCamera = _baseCameraResolver?.Invoke();
			if (baseCamera == null || baseCamera == _overlayCamera)
			{
				return;
			}

			var stack = baseCamera.GetUniversalAdditionalCameraData().cameraStack;
			if (stack.Contains(_overlayCamera))
			{
				_isStacked = true;
				return;
			}

			// A camera stacked by anything other than this feature sorts as priority 0.
			var priorities = new List<int>(stack.Count);
			foreach (var camera in stack)
			{
				priorities.Add(camera != null && _stackPriorities.TryGetValue(camera, out var known) ? known : 0);
			}

			var priority = StackPriority;
			stack.Insert(UiCameraStackRegistry.InsertIndex(priorities, priority), _overlayCamera);
			_stackPriorities[_overlayCamera] = priority;
			_isStacked = true;
		}

		private void RemoveFromStack()
		{
			if (!_isStacked || _overlayCamera == null)
			{
				_isStacked = false;
				return;
			}

			var baseCamera = _baseCameraResolver?.Invoke();
			if (baseCamera != null)
			{
				baseCamera.GetUniversalAdditionalCameraData().cameraStack.Remove(_overlayCamera);
			}

			_stackPriorities.Remove(_overlayCamera);
			_isStacked = false;
		}
	}
}
