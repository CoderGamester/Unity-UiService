using UnityEngine;

namespace GameLovers.UiService
{
	/// <summary>
	/// Rotates a presenter to match an authored camera or a cached active scene camera.
	/// </summary>
	public class UiBillboardFeature : PresenterFeatureBase
	{
		private const int CameraResolveRetryFrames = 30;

		[SerializeField] private Camera _camera;
		[SerializeField] private bool _lockYAxis = true;

		private Camera _cachedCamera;
		private int _nextCameraResolveFrame;

		/// <summary>The preferred camera, or null to use an active scene camera.</summary>
		public Camera Camera
		{
			get => _camera;
			set
			{
				_camera = value;
				_cachedCamera = null;
				_nextCameraResolveFrame = 0;
			}
		}

		/// <summary>Whether the presenter rotates only around the world Y axis.</summary>
		public bool LockYAxis
		{
			get => _lockYAxis;
			set => _lockYAxis = value;
		}

		private void LateUpdate()
		{
			var resolvedCamera = ResolveCamera();
			if (resolvedCamera == null)
			{
				return;
			}

			if (!_lockYAxis)
			{
				transform.rotation = resolvedCamera.transform.rotation;
				return;
			}

			var direction = resolvedCamera.transform.position - transform.position;
			direction.y = 0f;
			if (direction.sqrMagnitude <= Mathf.Epsilon)
			{
				return;
			}

			transform.rotation = Quaternion.LookRotation(-direction, Vector3.up);
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
	}
}
