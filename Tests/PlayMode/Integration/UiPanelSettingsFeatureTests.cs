using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace GameLovers.UiService.Tests.PlayMode
{
	/// <summary>
	/// Isolated feature tests construct a presenter GameObject directly and invoke
	/// OnPresenterInitialized() explicitly -- the same public entry point UiPresenter's
	/// InitializeFeatures() calls -- so the clone policy can be configured before
	/// initialization runs (not achievable through the full UiService loading pipeline,
	/// since Init() happens inside LoadUiAsync). Integration tests further down use the
	/// real UiService + MockAssetLoader pipeline where the behaviour under test is the
	/// integration itself (layer -> sorting order propagation, unload lifecycle).
	/// </summary>
	[TestFixture]
	public class UiPanelSettingsFeatureTests
	{
		private GameObject _go;
		private PanelSettings _sharedAsset;
		private UiToolkitPresenterFeature _toolkitFeature;
		private UiPanelSettingsFeature _feature;

		[SetUp]
		public void Setup()
		{
			_go = new GameObject(nameof(UiPanelSettingsFeatureTests));
			var document = _go.AddComponent<UIDocument>();

			_sharedAsset = ScriptableObject.CreateInstance<PanelSettings>();
			_sharedAsset.themeStyleSheet = ScriptableObject.CreateInstance<ThemeStyleSheet>();
			document.panelSettings = _sharedAsset;

			_toolkitFeature = _go.AddComponent<UiToolkitPresenterFeature>();
			_toolkitFeature.SetDocument(document);

			_feature = _go.AddComponent<UiPanelSettingsFeature>();
		}

		[TearDown]
		public void TearDown()
		{
			if (_go != null)
			{
				Object.DestroyImmediate(_go);
			}
		}

		[UnityTest]
		public IEnumerator UseSharedAsset_NeverClones_EvenWithOverrides()
		{
			var overrides = new UiPanelSettingsOverrides { OverrideClearColor = true, ClearColor = true };
			_feature.ConfigureForTest(UiPanelSettingsClonePolicy.UseSharedAsset, overrides);

			_feature.OnPresenterInitialized(null);

			Assert.IsFalse(_feature.IsClone);
			Assert.AreSame(_sharedAsset, _feature.ActivePanelSettings);
			Assert.AreSame(_sharedAsset, _toolkitFeature.Document.panelSettings);
			yield return null;
		}

		[UnityTest]
		public IEnumerator CloneWhenOverridden_NoOverrides_DoesNotClone()
		{
			_feature.ConfigureForTest(UiPanelSettingsClonePolicy.CloneWhenOverridden);

			_feature.OnPresenterInitialized(null);

			Assert.IsFalse(_feature.IsClone);
			Assert.AreSame(_sharedAsset, _feature.ActivePanelSettings);
			yield return null;
		}

		[UnityTest]
		public IEnumerator CloneWhenOverridden_WithOverrides_Clones_AndAppliesThem()
		{
			var overrides = new UiPanelSettingsOverrides
			{
				OverrideClearColor = true,
				ClearColor = true,
				OverrideReferenceResolution = true,
				ReferenceResolution = new Vector2Int(640, 360)
			};
			_feature.ConfigureForTest(UiPanelSettingsClonePolicy.CloneWhenOverridden, overrides);

			_feature.OnPresenterInitialized(null);

			Assert.IsTrue(_feature.IsClone);
			Assert.AreNotSame(_sharedAsset, _feature.ActivePanelSettings);
			Assert.IsTrue(_feature.ActivePanelSettings.clearColor);
			Assert.AreEqual(new Vector2Int(640, 360), _feature.ActivePanelSettings.referenceResolution);
			yield return null;
		}

		[UnityTest]
		public IEnumerator CloneAlways_ClonesEvenWithoutOverrides()
		{
			_feature.ConfigureForTest(UiPanelSettingsClonePolicy.CloneAlways);

			_feature.OnPresenterInitialized(null);

			Assert.IsTrue(_feature.IsClone);
			Assert.AreNotSame(_sharedAsset, _feature.ActivePanelSettings);
			yield return null;
		}

		[UnityTest]
		public IEnumerator SetTargetTexture_ForcesClone_AndSetsWorldSpace()
		{
			_feature.OnPresenterInitialized(null);
			Assert.IsFalse(_feature.IsClone, "Should start on the shared asset (no overrides, default policy).");

			var rt = new RenderTexture(64, 64, 0);
			_feature.SetTargetTexture(rt);

			Assert.IsTrue(_feature.IsClone);
			Assert.AreEqual(rt, _feature.ActivePanelSettings.targetTexture);
			Assert.AreEqual(PanelRenderMode.WorldSpace, _feature.ActivePanelSettings.renderMode);
			Assert.IsNull(_sharedAsset.targetTexture, "Shared asset must not receive the target texture.");

			rt.Release();
			Object.DestroyImmediate(rt);
			yield return null;
		}

		[UnityTest]
		public IEnumerator SetTargetTexture_Null_RevertsToScreenSpaceOverlay()
		{
			_feature.OnPresenterInitialized(null);

			var rt = new RenderTexture(64, 64, 0);
			_feature.SetTargetTexture(rt);
			_feature.SetTargetTexture(null);

			Assert.IsNull(_feature.ActivePanelSettings.targetTexture);
			Assert.AreEqual(PanelRenderMode.ScreenSpaceOverlay, _feature.ActivePanelSettings.renderMode);

			rt.Release();
			Object.DestroyImmediate(rt);
			yield return null;
		}

		[UnityTest]
		public IEnumerator Clone_MirrorsDocumentSortingOrder()
		{
			_toolkitFeature.Document.sortingOrder = 7;
			_feature.ConfigureForTest(UiPanelSettingsClonePolicy.CloneAlways);

			_feature.OnPresenterInitialized(null);

			Assert.AreEqual(_toolkitFeature.Document.sortingOrder, _feature.ActivePanelSettings.sortingOrder);
			yield return null;
		}

		[UnityTest]
		public IEnumerator SharedAsset_IsNeverMutated_AfterTargetTextureAndDestroy()
		{
			_feature.OnPresenterInitialized(null);

			var originalClearColor = _sharedAsset.clearColor;

			var rt = new RenderTexture(64, 64, 0);
			_feature.SetTargetTexture(rt);

			Assert.AreEqual(originalClearColor, _sharedAsset.clearColor);
			Assert.IsNull(_sharedAsset.targetTexture);

			Object.DestroyImmediate(_feature);
			yield return null;

			Assert.AreEqual(originalClearColor, _sharedAsset.clearColor, "Shared asset must be unmutated after the feature is destroyed.");
			Assert.IsNull(_sharedAsset.targetTexture);
			Assert.AreSame(_sharedAsset, _toolkitFeature.Document.panelSettings, "Document must be restored to the shared asset on destroy.");

			rt.Release();
			Object.DestroyImmediate(rt);
		}

		[UnityTest]
		public IEnumerator Clone_IsDestroyed_WhenFeatureIsDestroyed()
		{
			_feature.ConfigureForTest(UiPanelSettingsClonePolicy.CloneAlways);
			_feature.OnPresenterInitialized(null);

			var clone = _feature.ActivePanelSettings;
			Assert.IsTrue(_feature.IsClone);

			Object.DestroyImmediate(_feature);
			yield return null;

			Assert.IsTrue(clone == null, "Clone should be destroyed (Unity fake-null) once the feature is destroyed.");
		}
	}

	/// <summary>
	/// Integration coverage through the real UiService + MockAssetLoader pipeline, for the one
	/// thing the isolated tests above cannot exercise: that UiService.AddUi's layer -> sorting
	/// order propagation (which runs before OnPresenterInitialized) is what the clone picks up.
	/// </summary>
	[TestFixture]
	public class UiPanelSettingsFeatureIntegrationTests
	{
		private const string Address = "panelsettings_presenter";

		private MockAssetLoader _mockLoader;
		private UiService _service;

		[SetUp]
		public void Setup()
		{
			_mockLoader = new MockAssetLoader();
			_mockLoader.RegisterPrefab<TestPanelSettingsPresenter>(Address);

			_service = new UiService(_mockLoader);

			var configs = TestHelpers.CreateTestConfigs(
				TestHelpers.CreateTestConfig(typeof(TestPanelSettingsPresenter), Address, layer: 42)
			);
			_service.Init(configs);
		}

		[TearDown]
		public void TearDown()
		{
			_service?.Dispose();
			_mockLoader?.Cleanup();
		}

		[UnityTest]
		public IEnumerator LoadedPresenter_DefaultsToSharedAsset_NoOverridesConfigured()
		{
			var task = _service.LoadUiAsync(typeof(TestPanelSettingsPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestPanelSettingsPresenter;

			Assert.IsNotNull(presenter);
			Assert.IsFalse(presenter.PanelSettingsFeature.IsClone);
			Assert.AreSame(presenter.SharedPanelSettings, presenter.PanelSettingsFeature.ActivePanelSettings);
		}

		[UnityTest]
		public IEnumerator SharedAsset_IsNeverMutated_ThroughFullOpenCloseUnloadCycle()
		{
			var task = _service.OpenUiAsync(typeof(TestPanelSettingsPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestPanelSettingsPresenter;

			var sharedAsset = presenter.SharedPanelSettings;
			var originalClearColor = sharedAsset.clearColor;

			var rt = new RenderTexture(64, 64, 0);
			presenter.PanelSettingsFeature.SetTargetTexture(rt);

			Assert.IsNull(sharedAsset.targetTexture, "Shared asset must not receive the target texture.");

			_service.CloseUi<TestPanelSettingsPresenter>(destroy: true);
			yield return null;

			Assert.AreEqual(originalClearColor, sharedAsset.clearColor, "Shared asset must still be unmutated after unload.");
			Assert.IsNull(sharedAsset.targetTexture, "Shared asset must still be unmutated after unload.");

			rt.Release();
			Object.DestroyImmediate(rt);
		}
	}
}
