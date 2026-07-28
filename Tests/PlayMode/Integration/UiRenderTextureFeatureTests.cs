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
	/// UiPanelSettingsFeature and UiCameraStackFeature.
	/// </summary>
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
		public IEnumerator CanvasMode_WithoutRenderCamera_LogsError_DoesNotThrow()
		{
			_feature.ConfigureForTest(UiRenderTextureMode.Canvas);

			LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*requires a render Camera.*"));
			Assert.DoesNotThrow(() => _feature.OnPresenterInitialized(null));
			yield return null;
		}

		[UnityTest]
		public IEnumerator CanvasMode_AllocatesTexture_AndTargetsRenderCamera()
		{
			_feature.ConfigureForTest(UiRenderTextureMode.Canvas, renderCamera: _renderCamera,
				size: new Vector2Int(128, 128));

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
		public IEnumerator CanvasMode_AllocatedTexture_HasStencil()
		{
			_feature.ConfigureForTest(UiRenderTextureMode.Canvas, renderCamera: _renderCamera,
				size: new Vector2Int(128, 128), depthBits: 24);

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

			_feature.ConfigureForTest(UiRenderTextureMode.Canvas, presetTexture: preset, renderCamera: _renderCamera);
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
			_feature.ConfigureForTest(UiRenderTextureMode.Canvas, renderCamera: _renderCamera,
				size: new Vector2Int(64, 64));
			_feature.OnPresenterInitialized(null);
			_feature.OnPresenterOpening();

			var texture = _feature.Target;
			Assert.IsTrue(texture.IsCreated());

			Object.DestroyImmediate(_feature);
			yield return null;

			Assert.IsTrue(texture == null, "Owned RenderTexture must be destroyed (Unity fake-null) when the feature is destroyed.");
		}

		[UnityTest]
		public IEnumerator ToolkitMode_WithoutPanelSettingsFeature_LogsError()
		{
			var document = _go.AddComponent<UIDocument>();
			var toolkitFeature = _go.AddComponent<UiToolkitPresenterFeature>();
			toolkitFeature.SetDocument(document);

			_feature.ConfigureForTest(UiRenderTextureMode.UiToolkit);

			LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*requires a.*UiPanelSettingsFeature.*"));
			_feature.OnPresenterInitialized(null);
			yield return null;
		}

		[UnityTest]
		public IEnumerator ToolkitMode_RoutesThroughPanelSettingsFeature_NeverTouchesSharedAssetDirectly()
		{
			var document = _go.AddComponent<UIDocument>();
			var sharedAsset = ScriptableObject.CreateInstance<PanelSettings>();
			sharedAsset.themeStyleSheet = ScriptableObject.CreateInstance<ThemeStyleSheet>();
			document.panelSettings = sharedAsset;

			var toolkitFeature = _go.AddComponent<UiToolkitPresenterFeature>();
			toolkitFeature.SetDocument(document);

			var panelSettingsFeature = _go.AddComponent<UiPanelSettingsFeature>();
			panelSettingsFeature.OnPresenterInitialized(null);

			_feature.ConfigureForTest(UiRenderTextureMode.UiToolkit, size: new Vector2Int(64, 64));
			_feature.OnPresenterInitialized(null);
			_feature.OnPresenterOpening();

			Assert.IsNotNull(_feature.Target);
			Assert.AreEqual(_feature.Target, panelSettingsFeature.ActivePanelSettings.targetTexture);
			Assert.IsNull(sharedAsset.targetTexture, "Shared PanelSettings asset must never receive the target texture directly.");
			Assert.IsTrue(panelSettingsFeature.IsClone);
			yield return null;
		}

		[UnityTest]
		public IEnumerator AutoMode_ResolvesToUiToolkit_WhenPanelSettingsFeaturePresent()
		{
			var document = _go.AddComponent<UIDocument>();
			var sharedAsset = ScriptableObject.CreateInstance<PanelSettings>();
			sharedAsset.themeStyleSheet = ScriptableObject.CreateInstance<ThemeStyleSheet>();
			document.panelSettings = sharedAsset;

			var toolkitFeature = _go.AddComponent<UiToolkitPresenterFeature>();
			toolkitFeature.SetDocument(document);
			_go.AddComponent<UiPanelSettingsFeature>().OnPresenterInitialized(null);

			_feature.ConfigureForTest(UiRenderTextureMode.Auto, size: new Vector2Int(64, 64));
			_feature.OnPresenterInitialized(null);
			_feature.OnPresenterOpening();

			Assert.IsNotNull(_feature.Target, "Auto mode should have resolved to UiToolkit and produced a texture.");
			yield return null;
		}

		[UnityTest]
		public IEnumerator AutoMode_ResolvesToCanvas_WhenNoPanelSettingsFeature()
		{
			_feature.ConfigureForTest(UiRenderTextureMode.Auto, renderCamera: _renderCamera, size: new Vector2Int(64, 64));
			_feature.OnPresenterInitialized(null);
			_feature.OnPresenterOpening();

			Assert.AreSame(_feature.Target, _renderCamera.targetTexture);
			yield return null;
		}

		[UnityTest]
		public IEnumerator TargetChanged_InvokedOnApply()
		{
			RenderTexture received = null;
			_feature.TargetChanged.AddListener(t => received = t);

			_feature.ConfigureForTest(UiRenderTextureMode.Canvas, renderCamera: _renderCamera, size: new Vector2Int(64, 64));
			_feature.OnPresenterInitialized(null);
			_feature.OnPresenterOpening();

			Assert.IsNotNull(received);
			Assert.AreSame(_feature.Target, received);
			yield return null;
		}
	}
}
