using System.Collections;
using Cysharp.Threading.Tasks;
using GameLovers.UiService.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

namespace GameLovers.UiService.Tests.PlayMode
{
	/// <summary>
	/// Isolated feature tests construct the presenter GameObject directly (not through
	/// UiService/MockAssetLoader) and invoke OnPresenterInitialized/OnPresenterOpening
	/// explicitly -- the same public entry points UiPresenter calls -- so a
	/// a pinned base camera can be injected before initialization runs.
	/// </summary>
	[TestFixture]
	public class UiCameraStackFeatureTests
	{
		private GameObject _presenterGo;
		private Canvas _canvas;
		private GameObject _overlayCameraGo;
		private Camera _overlayCamera;
		private GameObject _baseCameraGo;
		private Camera _baseCamera;
		private UiCameraStackFeature _feature;

		[SetUp]
		public void Setup()
		{
			_presenterGo = new GameObject(nameof(UiCameraStackFeatureTests));
			_canvas = _presenterGo.AddComponent<Canvas>();

			_overlayCameraGo = new GameObject("OverlayCamera");
			_overlayCamera = _overlayCameraGo.AddComponent<Camera>();

			_baseCameraGo = new GameObject("BaseCamera");
			_baseCamera = _baseCameraGo.AddComponent<Camera>();

			_feature = _presenterGo.AddComponent<UiCameraStackFeature>();
			_feature.ConfigureForTest(_overlayCamera, _canvas, () => _baseCamera);
		}

		[TearDown]
		public void TearDown()
		{
			if (_presenterGo != null) Object.DestroyImmediate(_presenterGo);
			if (_overlayCameraGo != null) Object.DestroyImmediate(_overlayCameraGo);
			if (_baseCameraGo != null) Object.DestroyImmediate(_baseCameraGo);
		}

		[UnityTest]
		public IEnumerator OnPresenterInitialized_SetsCanvasToScreenSpaceCameraWithOverlayCamera()
		{
			_feature.OnPresenterInitialized(null);

			Assert.AreEqual(RenderMode.ScreenSpaceCamera, _canvas.renderMode);
			Assert.AreSame(_overlayCamera, _canvas.worldCamera);
			yield return null;
		}

		[UnityTest]
		public IEnumerator OnPresenterInitialized_SetsOverlayCameraAsUrpOverlay_AndDisablesIt()
		{
			_feature.OnPresenterInitialized(null);

			var data = _overlayCamera.GetUniversalAdditionalCameraData();
			Assert.AreEqual(CameraRenderType.Overlay, data.renderType);
			Assert.IsFalse(data.renderPostProcessing);
			Assert.IsFalse(_overlayCamera.enabled, "Overlay camera should be disabled -- driven entirely by the stack.");
			yield return null;
		}

		[UnityTest]
		public IEnumerator OnPresenterOpening_InsertsOverlayCameraIntoBaseStack()
		{
			_feature.OnPresenterInitialized(null);
			_feature.OnPresenterOpening();

			var baseData = _baseCamera.GetUniversalAdditionalCameraData();
			Assert.IsTrue(baseData.cameraStack.Contains(_overlayCamera));
			Assert.IsTrue(_feature.IsStacked);
			yield return null;
		}

		[UnityTest]
		public IEnumerator OnPresenterOpening_CalledTwice_DoesNotDoubleInsert()
		{
			_feature.OnPresenterInitialized(null);
			_feature.OnPresenterOpening();
			_feature.OnPresenterOpening();

			var baseData = _baseCamera.GetUniversalAdditionalCameraData();
			var count = 0;
			foreach (var cam in baseData.cameraStack)
			{
				if (cam == _overlayCamera) count++;
			}

			Assert.AreEqual(1, count, "Reopening must not insert the overlay camera twice.");
			yield return null;
		}

		[UnityTest]
		public IEnumerator DisablingPresenter_RemovesOverlayCameraFromStack()
		{
			_feature.OnPresenterInitialized(null);
			_feature.OnPresenterOpening();

			_presenterGo.SetActive(false); // fires OnDisable on all components, including the feature

			var baseData = _baseCamera.GetUniversalAdditionalCameraData();
			Assert.IsFalse(baseData.cameraStack.Contains(_overlayCamera));
			Assert.IsFalse(_feature.IsStacked);
			yield return null;
		}

		[UnityTest]
		public IEnumerator Reopen_AfterDisable_ReInsertsIntoStack()
		{
			_feature.OnPresenterInitialized(null);
			_feature.OnPresenterOpening();
			_presenterGo.SetActive(false);
			_presenterGo.SetActive(true);
			_feature.OnPresenterOpening();

			var baseData = _baseCamera.GetUniversalAdditionalCameraData();
			Assert.IsTrue(baseData.cameraStack.Contains(_overlayCamera));
			yield return null;
		}

		[UnityTest]
		public IEnumerator MultipleStackedCameras_OrderByCanvasSortingOrder()
		{
			_canvas.sortingOrder = 20;
			_feature.OnPresenterInitialized(null);
			_feature.OnPresenterOpening();

			// Second presenter stacking onto the same base camera, lower sorting order.
			var secondGo = new GameObject("SecondPresenter");
			var secondCanvas = secondGo.AddComponent<Canvas>();
			secondCanvas.sortingOrder = 5;
			var secondCameraGo = new GameObject("SecondOverlayCamera");
			var secondCamera = secondCameraGo.AddComponent<Camera>();
			var secondFeature = secondGo.AddComponent<UiCameraStackFeature>();
			secondFeature.ConfigureForTest(secondCamera, secondCanvas, () => _baseCamera);

			secondFeature.OnPresenterInitialized(null);
			secondFeature.OnPresenterOpening();

			var baseData = _baseCamera.GetUniversalAdditionalCameraData();
			var firstIndex = baseData.cameraStack.IndexOf(_overlayCamera);
			var secondIndex = baseData.cameraStack.IndexOf(secondCamera);

			Assert.Less(secondIndex, firstIndex, "Lower sortingOrder (5) should stack before higher (20).");

			Object.DestroyImmediate(secondGo);
			Object.DestroyImmediate(secondCameraGo);
			yield return null;
		}

		[UnityTest]
		public IEnumerator DestroyedBaseCamera_DoesNotThrow()
		{
			_feature.OnPresenterInitialized(null);
			_feature.OnPresenterOpening();

			Object.DestroyImmediate(_baseCameraGo);
			_baseCameraGo = null;

			Assert.DoesNotThrow(() => _presenterGo.SetActive(false));
			yield return null;
		}

		[UnityTest]
		public IEnumerator NullBaseCamera_OnPresenterOpening_DoesNotThrow_AndDoesNotStack()
		{
			_feature.ConfigureForTest(_overlayCamera, _canvas, () => null);
			_feature.OnPresenterInitialized(null);

			Assert.DoesNotThrow(() => _feature.OnPresenterOpening());
			Assert.IsFalse(_feature.IsStacked);
			yield return null;
		}
	}

	/// <summary>
	/// Light integration smoke test through the real UiService + MockAssetLoader pipeline,
	/// confirming the feature initializes without throwing when the default
	/// the default resolver (Camera.main) finds nothing in the test scene.
	/// </summary>
	[TestFixture]
	public class UiCameraStackFeatureIntegrationTests
	{
		private const string Address = "camerastack_presenter";

		private MockAssetLoader _mockLoader;
		private UiService _service;

		[SetUp]
		public void Setup()
		{
			_mockLoader = new MockAssetLoader();
			_mockLoader.RegisterPrefab<TestCameraStackPresenter>(Address);

			_service = new UiService(_mockLoader);
			_service.Init(TestHelpers.CreateTestConfigs(
				TestHelpers.CreateTestConfig(typeof(TestCameraStackPresenter), Address, layer: 0)));
		}

		[TearDown]
		public void TearDown()
		{
			_service?.Dispose();
			_mockLoader?.Cleanup();
		}

		[UnityTest]
		public IEnumerator LoadThroughUiService_DoesNotThrow_WithNoMainCameraInScene()
		{
			Assert.DoesNotThrow(() =>
			{
				var task = _service.OpenUiAsync(typeof(TestCameraStackPresenter));
				task.Forget();
			});

			yield return null;
			yield return null;

			var presenter = Object.FindAnyObjectByType<TestCameraStackPresenter>();
			Assert.IsNotNull(presenter);
			Assert.IsNotNull(presenter.StackFeature);
			Assert.IsFalse(presenter.StackFeature.IsStacked, "No Camera.main in the test scene, so nothing should be stacked.");
		}
	}
}
