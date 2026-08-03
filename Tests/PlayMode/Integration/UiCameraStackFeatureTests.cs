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
		// ADMIT: UiCameraStackFeature.OnPresenterInitialized must switch the canvas to ScreenSpaceCamera; an Overlay
		// canvas composites after all URP rendering and ignores the stacked camera entirely.
		// RCR: UiCameraStackFeature.cs OnPresenterInitialized — comment out
		// `_canvas.renderMode = RenderMode.ScreenSpaceCamera;` → RED (expected ScreenSpaceCamera, was Overlay). 2026-08-02
		public IEnumerator OnPresenterInitialized_SetsCanvasToScreenSpaceCameraWithOverlayCamera()
		{
			_feature.OnPresenterInitialized(null);

			Assert.AreEqual(RenderMode.ScreenSpaceCamera, _canvas.renderMode);
			Assert.AreSame(_overlayCamera, _canvas.worldCamera);
			yield return null;
		}

		[UnityTest]
		// ADMIT: UiCameraStackFeature.OnPresenterInitialized must set the overlay camera's URP renderType to Overlay,
		// or URP treats a stacked Base camera as an illegal stack entry and refuses to render it.
		// RCR: UiCameraStackFeature.cs OnPresenterInitialized — comment out
		// `overlayData.renderType = CameraRenderType.Overlay;` → RED (expected Overlay, was Base). 2026-08-02
		public IEnumerator OnPresenterInitialized_SetsOverlayCameraAsUrpOverlay_AndDisablesIt()
		{
			_feature.OnPresenterInitialized(null);

			var data = _overlayCamera.GetUniversalAdditionalCameraData();
			Assert.AreEqual(CameraRenderType.Overlay, data.renderType);
			Assert.IsFalse(data.renderPostProcessing);
			Assert.IsFalse(_overlayCamera.enabled, "Overlay camera stays disabled until stacked; URP does not render an Overlay camera outside a stack.");
			yield return null;
		}

		[UnityTest]
		// ADMIT: UiCameraStackFeature.InsertIntoStack must insert the overlay camera into the base camera's
		// cameraStack; nothing else puts the presenter's UI in front of the 3D scene.
		// RCR: UiCameraStackFeature.cs InsertIntoStack — comment out
		// `stack.Insert(InsertIndex(priorities, priority), _overlayCamera);` → RED (stack does not contain it). 2026-08-02
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
		// ADMIT: UiCameraStackFeature.RemoveFromStack must remove the overlay camera from the base camera's stack on
		// disable, or a closed presenter's camera keeps rendering as part of every subsequent frame.
		// RCR: UiCameraStackFeature.cs RemoveFromStack — comment out the
		// `...cameraStack.Remove(_overlayCamera);` call → RED (the stack still contains the overlay camera). 2026-08-02
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
		// ADMIT: UiCameraStackFeature.RemoveFromStack must clear _isStacked, or the reopen path short-circuits on its
		// own stale flag and the camera is never re-inserted.
		// RCR: UiCameraStackFeature.cs RemoveFromStack — comment out the trailing `_isStacked = false;` → RED
		// (the stack does not contain the overlay camera after reopening). 2026-08-02
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
		// ADMIT: UiCameraStackFeature.InsertIntoStack must enable the overlay camera — UniversalRenderPipeline
		// .RenderCameraStack skips a stacked camera that is not isActiveAndEnabled, so membership alone renders nothing.
		// RCR: UiCameraStackFeature.cs InsertIntoStack — comment out `_overlayCamera.enabled = true;` → RED
		// (Assert.IsTrue(_overlayCamera.enabled) fails after stacking). 2026-08-02
		public IEnumerator StackedCamera_IsEnabled_SoUrpActuallyRendersIt()
		{
			_feature.OnPresenterInitialized(null);

			// Before stacking it should stay disabled: an Overlay camera outside a stack renders nothing.
			Assert.IsFalse(_overlayCamera.enabled, "Overlay camera should be disabled until it is stacked.");

			_feature.OnPresenterOpening();

			// UniversalRenderPipeline.RenderCameraStack skips stacked overlay cameras that are not
			// isActiveAndEnabled, so membership alone does not mean the UI renders.
			Assert.IsTrue(_overlayCamera.enabled,
				"A stacked overlay camera MUST be enabled or URP silently renders nothing.");
			Assert.IsTrue(_overlayCamera.isActiveAndEnabled,
				"URP checks isActiveAndEnabled, so the GameObject must be active too.");
			yield return null;
		}

		[UnityTest]
		// ADMIT: UiCameraStackFeature.RemoveFromStack must disable the overlay camera once unstacked; an enabled
		// Overlay-type camera outside a stack is dead weight URP still walks every frame.
		// RCR: UiCameraStackFeature.cs RemoveFromStack — comment out `_overlayCamera.enabled = false;` → RED
		// (Assert.IsFalse(_overlayCamera.enabled) fails). Anchored past OnPresenterInitialized's identical line. 2026-08-02
		public IEnumerator UnstackedCamera_IsDisabledAgain_OnClose()
		{
			_feature.OnPresenterInitialized(null);
			_feature.OnPresenterOpening();
			Assert.IsTrue(_overlayCamera.enabled);

			_presenterGo.SetActive(false);

			Assert.IsFalse(_overlayCamera.enabled, "Overlay camera should be disabled once unstacked.");
			yield return null;
		}

		[UnityTest]
		// ADMIT: UiCameraStackFeature.DetachCameraFromCanvas re-parents the overlay camera out of its own canvas
		// — the half of commit 4f72d23 that no sibling test here covers (they pin enabled/isActiveAndEnabled).
		// RCR: UiCameraStackFeature.cs DetachCameraFromCanvas — delete the SetParent call and the three
		// localPosition/localRotation/localScale resets after it → RED (still a child, keeps its parented
		// offset). No exception: a Screen Space - Camera canvas drives a child camera onto the canvas plane,
		// so the symptom is an empty frame, not a crash. 2026-08-01
		public IEnumerator Open_WhenOverlayCameraIsChildOfItsOwnCanvas_DetachesCameraFromCanvas()
		{
			// Arrange -- parent the overlay camera under its own canvas, the authoring mistake this guards against.
			_overlayCameraGo.transform.SetParent(_canvas.transform, false);
			_overlayCameraGo.transform.localPosition = new Vector3(1f, 2f, 3f);
			Assume.That(_overlayCamera.transform.IsChildOf(_canvas.transform),
				"Precondition: the overlay camera must actually be parented under its own canvas for this test to exercise the detach path.");

			// Act
			_feature.OnPresenterInitialized(null);
			_feature.OnPresenterOpening();

			// Assert
			Assert.IsFalse(_overlayCamera.transform.IsChildOf(_canvas.transform),
				"DetachCameraFromCanvas must re-parent the camera out of its own canvas -- otherwise a Screen " +
				"Space - Camera canvas drives the camera's transform and it lands on the canvas plane, rendering nothing.");
			Assert.AreEqual(Vector3.zero, _overlayCamera.transform.localPosition,
				"Detach must reset local position to zero after re-parenting, or the camera keeps the offset it had as a child.");
			yield return null;
		}

		[UnityTest]
		// ADMIT: UiCameraStackFeature.StackPriority must derive from the canvas sortingOrder when _useCanvasSortingOrder
		// is set, or every presenter stacks at priority 0 and the layering collapses to insertion order.
		// RCR: UiCameraStackFeature.cs StackPriority — replace the property with `=> _stackPriority;` → RED
		// (Assert.Less(secondIndex, firstIndex) fails: sortingOrder 5 no longer stacks before 20). 2026-08-02
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
		// ADMIT: UiCameraStackFeature.RemoveFromStack's base-camera null check is what survives a base camera destroyed
		// while a presenter is still stacked — a routine scene-teardown ordering.
		// RCR: UiCameraStackFeature.cs RemoveFromStack — replace `if (baseCamera != null)` with `if (true)` → RED
		// (Assert.DoesNotThrow fails on GetUniversalAdditionalCameraData against a destroyed Camera). 2026-08-02
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
		// ADMIT: UiCameraStackFeature.InsertIntoStack's null base-camera guard is what lets a presenter open in a scene
		// with no resolvable base camera instead of throwing during the open lifecycle.
		// RCR: UiCameraStackFeature.cs InsertIntoStack — replace `if (baseCamera == null || baseCamera == _overlay...)`
		// with `if (baseCamera == _overlayCamera)` → RED (Assert.DoesNotThrow fails; IsStacked would be true). 2026-08-02
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
