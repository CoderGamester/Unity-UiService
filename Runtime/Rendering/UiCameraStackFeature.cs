using System;
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

		// Priority of every camera this feature currently has stacked. URP's cameraStack is a plain
		// List<Camera> with no priority of its own, so ordering a new camera against already-stacked
		// ones needs it recorded somewhere. Deliberately keyed by Camera rather than derived from the
		// hierarchy: the serialized _overlayCamera is not required to be a child of the presenter, so
		// walking up from it (GetComponentInParent) silently returns priority 0 for a camera assigned
		// anywhere else. Mirrors UiBackdropBlurRequests' static-registry pattern, reset included.
		private static readonly Dictionary<Camera, int> s_StackPriorities = new();

		private Func<Camera> _baseCameraResolver = DefaultBaseCameraResolver;
		private bool _isStacked;

		/// <summary>True while this presenter's overlay camera is inserted in the base camera's stack.</summary>
		public bool IsStacked => _isStacked;

		/// <summary>
		/// The stacking priority this presenter's overlay camera was inserted with. Read by sibling
		/// features to order themselves against an already-stacked camera.
		/// </summary>
		public int StackPriority => _useCanvasSortingOrder && _canvas != null ? _canvas.sortingOrder : _stackPriority;

		/// <summary>
		/// Swaps the base-camera resolution strategy. Defaults to <see cref="Camera.main"/>, which is the
		/// wrong answer in plenty of games (multiple cameras, no "MainCamera" tag, split-screen). Assign
		/// before the presenter opens; a null value restores the default.
		/// </summary>
		public Func<Camera> BaseCameraResolver
		{
			get => _baseCameraResolver;
			set => _baseCameraResolver = value ?? DefaultBaseCameraResolver;
		}

		// Camera.main is a cached tag lookup (measured at ~16ns/call) and this resolves twice per
		// open/close, so there is nothing here worth caching -- an earlier caching provider bought
		// no measurable time and leaked a static SceneManager.sceneLoaded subscription to do it.
		private static Camera DefaultBaseCameraResolver() => Camera.main;

		/// <summary>Test-only configuration hook (package tests have InternalsVisibleTo access).</summary>
		internal void ConfigureForTest(Camera overlayCamera, Canvas canvas, Func<Camera> baseCameraResolver = null,
			bool useCanvasSortingOrder = true, int stackPriority = 0)
		{
			_overlayCamera = overlayCamera;
			_canvas = canvas;
			_useCanvasSortingOrder = useCanvasSortingOrder;
			_stackPriority = stackPriority;
			_baseCameraResolver = baseCameraResolver ?? DefaultBaseCameraResolver;
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

			var baseCamera = _baseCameraResolver?.Invoke();
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

			// A camera stacked by anything other than this feature sorts as priority 0.
			var priorities = new List<int>(stack.Count);
			foreach (var camera in stack)
			{
				priorities.Add(camera != null && s_StackPriorities.TryGetValue(camera, out var known) ? known : 0);
			}

			var priority = StackPriority;
			var index = UiCameraStackRegistry.InsertIndex(priorities, priority);
			stack.Insert(index, _overlayCamera);
			s_StackPriorities[_overlayCamera] = priority;

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

			var baseCamera = _baseCameraResolver?.Invoke();
			if (baseCamera != null)
			{
				var baseData = baseCamera.GetUniversalAdditionalCameraData();
				baseData.cameraStack.Remove(_overlayCamera);
			}

			s_StackPriorities.Remove(_overlayCamera);
			_isStacked = false;
		}

		// With Domain Reload disabled, statics survive between play sessions -- without this a
		// second session would start with priorities recorded for destroyed cameras.
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetOnLoad()
		{
			s_StackPriorities.Clear();
		}
	}
}
