using System;
using UnityEngine;

namespace GameLovers.UiService
{
	/// <summary>
	/// Suspends presenter rendering outside a camera-distance band without deactivating the presenter.
	/// </summary>
	public class UiDistanceVisibilityFeature : PresenterFeatureBase
	{
		private const int CameraResolveRetryFrames = 30;

		[SerializeField] private Transform _target;
		[SerializeField] private Camera _camera;
		[SerializeField, Min(0f)] private float _maxDistance = 50f;
		[SerializeField, Min(0f)] private float _hysteresis = 1f;
		[SerializeField] private Renderer[] _renderers = Array.Empty<Renderer>();

		private Camera _cachedCamera;
		private IUiRenderActivity _renderActivity;
		private bool _hasVisibilityState;
		private bool _isVisible = true;
		private int _nextCameraResolveFrame;

		/// <summary>The position tested against the camera, or this presenter when null.</summary>
		public Transform Target
		{
			get => _target;
			set
			{
				_target = value;
				EvaluateVisibility(true);
			}
		}

		/// <summary>The preferred camera, or null to use an active scene camera.</summary>
		public Camera Camera
		{
			get => _camera;
			set
			{
				_camera = value;
				_cachedCamera = null;
				_nextCameraResolveFrame = 0;
				EvaluateVisibility(true);
			}
		}

		/// <summary>The center distance of the visibility hysteresis band.</summary>
		public float MaxDistance
		{
			get => _maxDistance;
			set
			{
				_maxDistance = Mathf.Max(0f, value);
				EvaluateVisibility(true);
			}
		}

		/// <summary>The distance added when hiding and subtracted when becoming visible again.</summary>
		public float Hysteresis
		{
			get => _hysteresis;
			set
			{
				_hysteresis = Mathf.Max(0f, value);
				EvaluateVisibility(true);
			}
		}

		/// <summary>Whether renderer output and backend render work are currently enabled.</summary>
		public bool IsVisible => !_hasVisibilityState || _isVisible;

		/// <summary>The optional backend-specific render-work controller.</summary>
		public IUiRenderActivity RenderActivity
		{
			get => _renderActivity;
			set
			{
				_renderActivity = value;
				if (_hasVisibilityState)
				{
					_renderActivity?.SetRenderActive(_isVisible);
				}
			}
		}

		private void OnValidate()
		{
			_maxDistance = Mathf.Max(0f, _maxDistance);
			_hysteresis = Mathf.Max(0f, _hysteresis);
		}

		private void LateUpdate()
		{
			EvaluateVisibility(false);
		}

		/// <inheritdoc />
		public override void OnPresenterInitialized(UiPresenter presenter)
		{
			base.OnPresenterInitialized(presenter);
			if (_renderers == null || _renderers.Length == 0)
			{
				_renderers = GetComponentsInChildren<Renderer>(true);
			}

			if (_renderActivity == null)
			{
				var behaviours = GetComponents<MonoBehaviour>();
				foreach (var behaviour in behaviours)
				{
					if (behaviour is IUiRenderActivity activity)
					{
						_renderActivity = activity;
						break;
					}
				}
			}

			EvaluateVisibility(true);
		}

		private void EvaluateVisibility(bool force)
		{
			var resolvedCamera = ResolveCamera();
			if (resolvedCamera == null)
			{
				return;
			}

			var observedTransform = _target != null ? _target : transform;
			var threshold = !_hasVisibilityState
				? _maxDistance
				: _isVisible
					? _maxDistance + _hysteresis
					: Mathf.Max(0f, _maxDistance - _hysteresis);
			var offset = observedTransform.position - resolvedCamera.transform.position;
			var isVisible = offset.sqrMagnitude <= threshold * threshold;

			if (!force && _hasVisibilityState && isVisible == _isVisible)
			{
				return;
			}

			_hasVisibilityState = true;
			_isVisible = isVisible;
			SetRenderActive(isVisible);
		}

		private Camera ResolveCamera()
		{
			if (_camera != null && _camera.isActiveAndEnabled && _cachedCamera != _camera)
			{
				_cachedCamera = _camera;
			}

			if (_cachedCamera != null && _cachedCamera.isActiveAndEnabled)
			{
				return _cachedCamera;
			}

			if (Time.frameCount < _nextCameraResolveFrame)
			{
				return null;
			}

			_cachedCamera = UiFeatureCameraResolver.Resolve(_camera);
			_nextCameraResolveFrame = Time.frameCount + CameraResolveRetryFrames;
			return _cachedCamera;
		}

		private void SetRenderActive(bool isActive)
		{
			if (_renderers != null)
			{
				foreach (var renderer in _renderers)
				{
					if (renderer != null)
					{
						renderer.enabled = isActive;
					}
				}
			}

			_renderActivity?.SetRenderActive(isActive);
		}
	}
}
