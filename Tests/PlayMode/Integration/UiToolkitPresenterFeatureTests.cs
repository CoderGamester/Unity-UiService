using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace GameLovers.UiService.Tests.PlayMode
{
	[TestFixture]
	public class UiToolkitPresenterFeatureTests
	{
		private MockAssetLoader _mockLoader;
		private UiService _service;

		[SetUp]
		public void Setup()
		{
			_mockLoader = new MockAssetLoader();
			_mockLoader.RegisterPrefab<TestUiToolkitPresenter>("uitoolkit_presenter");

			_service = new UiService(_mockLoader);

			var configs = TestHelpers.CreateTestConfigs(
				TestHelpers.CreateTestConfig(typeof(TestUiToolkitPresenter), "uitoolkit_presenter", 0)
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
		public IEnumerator UiToolkitFeature_Document_IsAssigned()
		{
			var task = _service.LoadUiAsync(typeof(TestUiToolkitPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestUiToolkitPresenter;

			Assert.IsNotNull(presenter);
			Assert.IsNotNull(presenter.ToolkitFeature);
			Assert.IsNotNull(presenter.ToolkitFeature.Document);
		}

		[UnityTest]
		public IEnumerator UiToolkitFeature_Root_ReturnsRootVisualElement()
		{
			var task = _service.LoadUiAsync(typeof(TestUiToolkitPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestUiToolkitPresenter;

			Assert.IsNotNull(presenter);
			Assert.DoesNotThrow(() => { var _ = presenter.ToolkitFeature.Root; });
		}

		[UnityTest]
		public IEnumerator UiToolkitFeature_LifecycleHooks_AreCalled()
		{
			var task = _service.OpenUiAsync(typeof(TestUiToolkitPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestUiToolkitPresenter;

			Assert.IsNotNull(presenter);
			Assert.IsTrue(presenter.WasOpened);
		}

		[UnityTest]
		public IEnumerator UiToolkitFeature_WithMultipleFeatures_AllFeaturesWork()
		{
			_mockLoader.RegisterPrefab<TestMultiFeatureToolkitPresenter>("multi_feature");
			_service.AddUiConfig(TestHelpers.CreateTestConfig(typeof(TestMultiFeatureToolkitPresenter), "multi_feature", 0));

			var task = _service.OpenUiAsync(typeof(TestMultiFeatureToolkitPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestMultiFeatureToolkitPresenter;

			Assert.IsNotNull(presenter);
			Assert.IsNotNull(presenter.ToolkitFeature);
			Assert.IsNotNull(presenter.DelayFeature);
		}

		[UnityTest]
		// ADMIT: UiToolkitPresenterFeature.AddVisualTreeAttachedListener must register the callback on the
		// _onVisualTreeReady event, or no presenter ever gets a visual-tree-ready notification.
		// RCR: UiToolkitPresenterFeature.cs AddVisualTreeAttachedListener — comment out
		// `_onVisualTreeReady.AddListener(callback);` → RED (SetupCallbackCount never rises above 0). 2026-08-02
		public IEnumerator UiToolkitFeature_ListenerInvokedOnEachOpen()
		{
			var task = _service.OpenUiAsync(typeof(TestUiToolkitPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestUiToolkitPresenter;

			yield return TestHelpers.WaitForPanelAttachment(presenter);

			var initialCallbackCount = presenter.SetupCallbackCount;

			_service.CloseUi<TestUiToolkitPresenter>();
			yield return null;

			var reopenTask = _service.OpenUiAsync(typeof(TestUiToolkitPresenter));
			yield return reopenTask.ToCoroutine();

			yield return TestHelpers.WaitForPanelAttachment(presenter);

			Assert.Greater(presenter.SetupCallbackCount, initialCallbackCount);
		}

		[UnityTest]
		// ADMIT: UiToolkitPresenterFeature.RemoveVisualTreeAttachedListener must actually unsubscribe, or a presenter
		// that unregisters still gets called on every reopen and re-queries a stale visual tree.
		// RCR: UiToolkitPresenterFeature.cs RemoveVisualTreeAttachedListener — comment out
		// `_onVisualTreeReady.RemoveListener(callback);` → RED (SetupCallbackCount rises after the reopen). 2026-08-02
		public IEnumerator UiToolkitFeature_RemoveListener_StopsInvocation()
		{
			var task = _service.OpenUiAsync(typeof(TestUiToolkitPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestUiToolkitPresenter;

			yield return TestHelpers.WaitForPanelAttachment(presenter);

			presenter.RemoveSetupListener();
			var countAfterRemove = presenter.SetupCallbackCount;

			_service.CloseUi<TestUiToolkitPresenter>();
			yield return null;

			var reopenTask = _service.OpenUiAsync(typeof(TestUiToolkitPresenter));
			yield return reopenTask.ToCoroutine();

			yield return TestHelpers.WaitForPanelAttachment(presenter);

			Assert.AreEqual(countAfterRemove, presenter.SetupCallbackCount);
		}

		[UnityTest]
		public IEnumerator AddVisualTreeAttachedListener_NullCallback_NoOpsInsteadOfThrowing()
		{
			var task = _service.LoadUiAsync(typeof(TestUiToolkitPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestUiToolkitPresenter;
			Assert.IsNotNull(presenter?.ToolkitFeature);

			Assert.DoesNotThrow(() => presenter.ToolkitFeature.AddVisualTreeAttachedListener(null));
		}

		[UnityTest]
		// ADMIT: UiToolkitPresenterFeature.RemoveVisualTreeAttachedListener's null guard keeps a defensive unsubscribe
		// of a never-assigned callback from throwing during teardown.
		// RCR: UiToolkitPresenterFeature.cs RemoveVisualTreeAttachedListener — replace `if (callback == null)` with
		// `if (false)` → RED (Assert.DoesNotThrow fails inside UnityEvent.RemoveListener(null)). 2026-08-02
		public IEnumerator RemoveVisualTreeAttachedListener_NullCallback_NoOpsInsteadOfThrowing()
		{
			var task = _service.LoadUiAsync(typeof(TestUiToolkitPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestUiToolkitPresenter;
			Assert.IsNotNull(presenter?.ToolkitFeature);

			Assert.DoesNotThrow(() => presenter.ToolkitFeature.RemoveVisualTreeAttachedListener(null));
		}

		[UnityTest]
		// ADMIT: UiService.EnsureCanvasSortingOrder's UIDocument branch must assign document.sortingOrder = layer,
		// or a UI Toolkit presenter's panel keeps Unity's default 0 regardless of UiConfig.Layer.
		// RCR: UiService.cs EnsureCanvasSortingOrder — comment out `document.sortingOrder = layer;` → RED
		// (expected 4, was 0). 2026-08-01
		public IEnumerator LoadUiAsync_WithUiDocumentPresenter_SetsDocumentSortingOrderToLayer()
		{
			// Arrange - register a second UI Toolkit presenter config on a non-default layer
			_mockLoader.RegisterPrefab<TestMultiFeatureToolkitPresenter>("uitoolkit_layered");
			_service.AddUiConfig(TestHelpers.CreateTestConfig(typeof(TestMultiFeatureToolkitPresenter), "uitoolkit_layered", 4));

			// Act
			var task = _service.LoadUiAsync(typeof(TestMultiFeatureToolkitPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestMultiFeatureToolkitPresenter;

			// Assert
			var document = presenter.GetComponent<UnityEngine.UIElements.UIDocument>();
			Assert.AreEqual(4, document.sortingOrder);
		}
	}
}
