using GameLovers.UiService.Rendering;
using UnityEngine;

namespace GameLovers.UiService.Tests.PlayMode
{
	/// <summary>Fixed-camera IUiBaseCameraProvider test double -- avoids depending on Camera.main / tags in the test scene.</summary>
	public class FixedBaseCameraProvider : IUiBaseCameraProvider
	{
		public Camera Camera;

		public FixedBaseCameraProvider(Camera camera)
		{
			Camera = camera;
		}

		public Camera GetBaseCamera() => Camera;
	}

	/// <summary>
	/// Test presenter pairing a Canvas with a UiCameraStackFeature and a child overlay Camera.
	/// Caller must call ConfigureBaseCamera(...) before OnPresenterInitialized runs (i.e. before
	/// loading through UiService) to inject a FixedBaseCameraProvider instead of relying on Camera.main.
	/// </summary>
	[RequireComponent(typeof(Canvas))]
	[RequireComponent(typeof(UiCameraStackFeature))]
	public class TestCameraStackPresenter : UiPresenter
	{
		public Canvas Canvas { get; private set; }
		public Camera OverlayCamera { get; private set; }
		public UiCameraStackFeature StackFeature { get; private set; }

		private const string OverlayCameraChildName = "OverlayCamera";

		private void Awake()
		{
			// MockAssetLoader.RegisterPrefab<T>() calls Awake once while building the template
			// GameObject, then Object.Instantiate(prefab) calls it again on every loaded copy --
			// and Instantiate deep-copies child GameObjects (unlike plain C# properties, which
			// are not serialized/copied). Guard by name instead of unconditionally creating a
			// new child, or every instantiated copy ends up with two overlay cameras.
			Canvas = GetComponent<Canvas>();
			if (Canvas == null)
			{
				Canvas = gameObject.AddComponent<Canvas>();
			}

			var existingCameraChild = transform.Find(OverlayCameraChildName);
			if (existingCameraChild != null)
			{
				OverlayCamera = existingCameraChild.GetComponent<Camera>();
			}
			else
			{
				var cameraGo = new GameObject(OverlayCameraChildName);
				cameraGo.transform.SetParent(transform, false);
				OverlayCamera = cameraGo.AddComponent<Camera>();
			}

			StackFeature = GetComponent<UiCameraStackFeature>();
			if (StackFeature == null)
			{
				StackFeature = gameObject.AddComponent<UiCameraStackFeature>();
			}

			StackFeature.ConfigureForTest(OverlayCamera, Canvas);
		}

		/// <summary>Must be called before the presenter is loaded/opened through UiService.</summary>
		public void ConfigureBaseCamera(Camera baseCamera, bool useCanvasSortingOrder = true, int stackPriority = 0)
		{
			StackFeature.ConfigureForTest(OverlayCamera, Canvas, new FixedBaseCameraProvider(baseCamera),
				useCanvasSortingOrder, stackPriority);
		}
	}
}
