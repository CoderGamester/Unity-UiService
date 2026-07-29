using System.Collections;
using GameLovers.UiService.Rendering;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GameLovers.UiService.Tests.PlayMode
{
	/// <summary>
	/// Isolated feature tests construct the presenter GameObject directly and invoke
	/// OnPresenterInitialized/OnPresenterOpening explicitly, matching the pattern used for
	/// UiCameraStackFeature.
	/// </summary>
	/// <remarks>
	/// This feature is uGUI-only by design -- UI Toolkit world-space UI goes through
	/// PanelSettings.renderMode = PanelRenderMode.WorldSpace, not a render texture. The
	/// UiToolkitPresenter_LogsError test pins that boundary.
	/// </remarks>
	[TestFixture]
	public class UiRenderTextureFeatureTests
	{
		private GameObject _go;
		private Canvas _canvas;
		private GameObject _renderCameraGo;
		private Camera _renderCamera;
		private UiRenderTextureFeature _feature;

		[SetUp]
		public void Setup()
		{
			_go = new GameObject(nameof(UiRenderTextureFeatureTests));
			_canvas = _go.AddComponent<Canvas>();

			_renderCameraGo = new GameObject("RenderCamera");
			_renderCamera = _renderCameraGo.AddComponent<Camera>();

			_feature = _go.AddComponent<UiRenderTextureFeature>();
		}

		[TearDown]
		public void TearDown()
		{
			if (_go != null) Object.DestroyImmediate(_go);
			if (_renderCameraGo != null) Object.DestroyImmediate(_renderCameraGo);
		}

		[UnityTest]
		public IEnumerator WithoutRenderCamera_LogsError_DoesNotThrow()
		{
			_feature.ConfigureForTest();

			LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*requires a render Camera.*"));
			Assert.DoesNotThrow(() => _feature.OnPresenterInitialized(null));
			yield return null;
		}

		[UnityTest]
		public IEnumerator UiToolkitPresenter_LogsError_PointingAtWorldSpaceRenderMode()
		{
			// A UI Toolkit presenter has a UIDocument and no Canvas -- this feature must refuse it and
			// name the supported alternative rather than silently doing nothing.
			var toolkitGo = new GameObject("ToolkitPresenter");
			var document = toolkitGo.AddComponent<UIDocument>();
			var toolkitFeature = toolkitGo.AddComponent<UiToolkitPresenterFeature>();
			toolkitFeature.SetDocument(document);

			var feature = toolkitGo.AddComponent<UiRenderTextureFeature>();
			feature.ConfigureForTest(renderCamera: _renderCamera);

			LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*requires a Canvas.*WorldSpace.*"));
			feature.OnPresenterInitialized(null);

			Object.DestroyImmediate(toolkitGo);
			yield return null;
		}

		[UnityTest]
		public IEnumerator AllocatesTexture_AndTargetsRenderCamera()
		{
			_feature.ConfigureForTest(renderCamera: _renderCamera, size: new Vector2Int(128, 128));

			_feature.OnPresenterInitialized(null);
			_feature.OnPresenterOpening();

			Assert.IsNotNull(_feature.Target);
			Assert.IsTrue(_feature.Target.IsCreated());
			Assert.AreSame(_feature.Target, _renderCamera.targetTexture);
			Assert.AreEqual(RenderMode.ScreenSpaceCamera, _canvas.renderMode);
			Assert.AreSame(_renderCamera, _canvas.worldCamera);
			Assert.IsTrue(_feature.OwnsTarget);
			yield return null;
		}

		[UnityTest]
		public IEnumerator AllocatedTexture_HasStencil()
		{
			_feature.ConfigureForTest(renderCamera: _renderCamera, size: new Vector2Int(128, 128), depthBits: 24);

			_feature.OnPresenterInitialized(null);
			_feature.OnPresenterOpening();

			Assert.IsTrue(GraphicsFormatUtility.IsStencilFormat(_feature.Target.depthStencilFormat),
				"RenderTexture must carry a stencil format -- required for uGUI Mask/RectMask2D.");
			yield return null;
		}

		[UnityTest]
		public IEnumerator PresetTexture_IsUsedDirectly_AndNotOwned()
		{
			var preset = new RenderTexture(64, 64, 24) { name = "Preset" };
			preset.Create();

			_feature.ConfigureForTest(presetTexture: preset, renderCamera: _renderCamera);
			_feature.OnPresenterInitialized(null);
			_feature.OnPresenterOpening();

			Assert.AreSame(preset, _feature.Target);
			Assert.IsFalse(_feature.OwnsTarget, "A pre-authored asset must not be marked as owned.");

			// Destroying the feature must not release/destroy an asset it does not own.
			Object.DestroyImmediate(_feature);
			Assert.IsTrue(preset.IsCreated(), "Preset RenderTexture must survive feature destruction.");

			preset.Release();
			Object.DestroyImmediate(preset);
			yield return null;
		}

		[UnityTest]
		public IEnumerator OwnedTexture_IsReleased_OnDestroy()
		{
			_feature.ConfigureForTest(renderCamera: _renderCamera, size: new Vector2Int(64, 64));
			_feature.OnPresenterInitialized(null);
			_feature.OnPresenterOpening();

			var texture = _feature.Target;
			Assert.IsTrue(texture.IsCreated());

			Object.DestroyImmediate(_feature);
			yield return null;

			Assert.IsTrue(texture == null, "Owned RenderTexture must be destroyed (Unity fake-null) when the feature is destroyed.");
		}

		[UnityTest]
		public IEnumerator TargetChanged_InvokedOnApply()
		{
			RenderTexture received = null;
			_feature.TargetChanged.AddListener(t => received = t);

			_feature.ConfigureForTest(renderCamera: _renderCamera, size: new Vector2Int(64, 64));
			_feature.OnPresenterInitialized(null);
			_feature.OnPresenterOpening();

			Assert.IsNotNull(received);
			Assert.AreSame(_feature.Target, received);
			yield return null;
		}
	}
}
