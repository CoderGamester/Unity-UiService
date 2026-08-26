using UnityEngine;

namespace GameLovers.UiService
{
	/// <summary>Resolves an active camera for presenter features.</summary>
	internal static class UiFeatureCameraResolver
	{
		/// <summary>Returns the preferred camera or the best active scene fallback.</summary>
		public static Camera Resolve(Camera preferredCamera)
		{
			if (preferredCamera != null && preferredCamera.isActiveAndEnabled)
			{
				return preferredCamera;
			}

			var mainCamera = Camera.main;
			if (mainCamera != null && mainCamera.isActiveAndEnabled)
			{
				return mainCamera;
			}

			var cameras = Camera.allCameras;
			foreach (var camera in cameras)
			{
				if (camera != null && camera.isActiveAndEnabled)
				{
					return camera;
				}
			}

			return null;
		}
	}
}
