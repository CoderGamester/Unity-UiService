using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace GameLovers.UiService.Tests.PlayMode
{
	/// <summary>
	/// Tests for UiToolkitPresenterFeature functionality.
	/// </summary>
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
			// Act
			var task = _service.LoadUiAsync(typeof(TestUiToolkitPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestUiToolkitPresenter;

			// Assert
			Assert.IsNotNull(presenter);
			Assert.IsNotNull(presenter.ToolkitFeature);
			Assert.IsNotNull(presenter.ToolkitFeature.Document);
		}

		[UnityTest]
		public IEnumerator UiToolkitFeature_Root_ReturnsRootVisualElement()
		{
			// Act
			var task = _service.LoadUiAsync(typeof(TestUiToolkitPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestUiToolkitPresenter;

			// Assert - Root is the document's rootVisualElement
			// Note: May be null if no panel is assigned, but should not throw
			Assert.IsNotNull(presenter);
			Assert.DoesNotThrow(() => { var _ = presenter.ToolkitFeature.Root; });
		}

		[UnityTest]
		public IEnumerator UiToolkitFeature_LifecycleHooks_AreCalled()
		{
			// Arrange
			var task = _service.OpenUiAsync(typeof(TestUiToolkitPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestUiToolkitPresenter;

			// Assert - Feature lifecycle should have been invoked
			Assert.IsNotNull(presenter);
			Assert.IsTrue(presenter.WasOpened);
		}

		[UnityTest]
		public IEnumerator UiToolkitFeature_WithMultipleFeatures_AllFeaturesWork()
		{
			// Arrange - Register presenter with multiple features
			_mockLoader.RegisterPrefab<TestMultiFeatureToolkitPresenter>("multi_feature");
			_service.AddUiConfig(TestHelpers.CreateTestConfig(typeof(TestMultiFeatureToolkitPresenter), "multi_feature", 0));

			// Act
			var task = _service.OpenUiAsync(typeof(TestMultiFeatureToolkitPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestMultiFeatureToolkitPresenter;

			// Assert
			Assert.IsNotNull(presenter);
			Assert.IsNotNull(presenter.ToolkitFeature);
			Assert.IsNotNull(presenter.DelayFeature);
		}
	}
}
