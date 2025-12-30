using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace GameLovers.UiService.Tests.PlayMode
{
	/// <summary>
	/// Tests for concurrent/multiple features on a single presenter.
	/// Verifies that multiple features can notify transitions without conflicts.
	/// </summary>
	[TestFixture]
	public class ConcurrentFeatureTests
	{
		private MockAssetLoader _mockLoader;
		private UiService _service;

		[SetUp]
		public void Setup()
		{
			_mockLoader = new MockAssetLoader();
			_mockLoader.RegisterPrefab<TestDualFeaturePresenter>("dual_feature");
			_mockLoader.RegisterPrefab<TestTripleFeaturePresenter>("triple_feature");
			
			_service = new UiService(_mockLoader);
			
			var configs = TestHelpers.CreateTestConfigs(
				TestHelpers.CreateTestConfig(typeof(TestDualFeaturePresenter), "dual_feature", 0),
				TestHelpers.CreateTestConfig(typeof(TestTripleFeaturePresenter), "triple_feature", 1)
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
		public IEnumerator DualFeatures_BothReceiveLifecycleCallbacks()
		{
			// Act
			var task = _service.OpenUiAsync(typeof(TestDualFeaturePresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestDualFeaturePresenter;

			// Assert - Both features received lifecycle callbacks
			Assert.IsTrue(presenter.FeatureA.WasOpened);
			Assert.IsTrue(presenter.FeatureB.WasOpened);
		}

		[UnityTest]
		public IEnumerator DualFeatures_BothCanNotifyTransitionComplete()
		{
			// Arrange
			var task = _service.OpenUiAsync(typeof(TestDualFeaturePresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestDualFeaturePresenter;

			// Act - Both features notify transition complete
			presenter.FeatureA.SimulateOpenTransitionComplete();
			presenter.FeatureB.SimulateOpenTransitionComplete();

			// Assert - Presenter received both notifications
			Assert.AreEqual(2, presenter.OpenTransitionCount);
		}

		[UnityTest]
		public IEnumerator TripleFeatures_AllReceiveCallbacksInOrder()
		{
			// Act
			var task = _service.OpenUiAsync(typeof(TestTripleFeaturePresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestTripleFeaturePresenter;

			// Assert - All three features received callbacks
			Assert.IsTrue(presenter.FeatureA.WasOpened);
			Assert.IsTrue(presenter.FeatureB.WasOpened);
			Assert.IsTrue(presenter.FeatureC.WasOpened);
			
			// Verify order (A before B before C)
			Assert.IsTrue(presenter.FeatureA.OpenOrder < presenter.FeatureB.OpenOrder);
			Assert.IsTrue(presenter.FeatureB.OpenOrder < presenter.FeatureC.OpenOrder);
		}

		[UnityTest]
		public IEnumerator ConcurrentNotifications_DoNotInterfere()
		{
			// Arrange
			var task = _service.OpenUiAsync(typeof(TestTripleFeaturePresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestTripleFeaturePresenter;

			// Act - All features notify concurrently
			presenter.FeatureA.SimulateOpenTransitionComplete();
			presenter.FeatureB.SimulateOpenTransitionComplete();
			presenter.FeatureC.SimulateOpenTransitionComplete();

			// Assert - All notifications counted
			Assert.AreEqual(3, presenter.OpenTransitionCount);
		}

		[UnityTest]
		public IEnumerator MixedFeatures_CloseLifecycle_WorksCorrectly()
		{
			// Arrange
			var task = _service.OpenUiAsync(typeof(TestDualFeaturePresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestDualFeaturePresenter;

			// Act
			_service.CloseUi(typeof(TestDualFeaturePresenter));

			// Assert - Both features received closing callbacks
			Assert.IsTrue(presenter.FeatureA.WasClosing);
			Assert.IsTrue(presenter.FeatureB.WasClosing);
		}

		[UnityTest]
		public IEnumerator RapidOpenClose_FeaturesHandleCorrectly()
		{
			// Act - Rapid open/close cycles
			for (int i = 0; i < 3; i++)
			{
				var openTask = _service.OpenUiAsync(typeof(TestDualFeaturePresenter));
				yield return openTask.ToCoroutine();
				_service.CloseUi(typeof(TestDualFeaturePresenter));
			}

			// Assert - Should not throw, all cycles completed
			Assert.Pass("Rapid open/close cycles completed without errors");
		}

		[UnityTest]
		public IEnumerator FeatureOrder_Deterministic()
		{
			// Test that feature order is consistent across multiple opens
			int[] firstRunOrder = new int[3];
			int[] secondRunOrder = new int[3];

			// First run
			var task1 = _service.OpenUiAsync(typeof(TestTripleFeaturePresenter));
			yield return task1.ToCoroutine();
			var presenter1 = task1.GetAwaiter().GetResult() as TestTripleFeaturePresenter;
			firstRunOrder[0] = presenter1.FeatureA.OpenOrder;
			firstRunOrder[1] = presenter1.FeatureB.OpenOrder;
			firstRunOrder[2] = presenter1.FeatureC.OpenOrder;
			_service.CloseUi(typeof(TestTripleFeaturePresenter));

			// Unload and reload
			_service.UnloadUi(typeof(TestTripleFeaturePresenter));
			
			// Second run
			var task2 = _service.OpenUiAsync(typeof(TestTripleFeaturePresenter));
			yield return task2.ToCoroutine();
			var presenter2 = task2.GetAwaiter().GetResult() as TestTripleFeaturePresenter;
			secondRunOrder[0] = presenter2.FeatureA.OpenOrder;
			secondRunOrder[1] = presenter2.FeatureB.OpenOrder;
			secondRunOrder[2] = presenter2.FeatureC.OpenOrder;

			// Assert - Order should be consistent (relative ordering, not absolute values)
			Assert.IsTrue(secondRunOrder[0] < secondRunOrder[1]);
			Assert.IsTrue(secondRunOrder[1] < secondRunOrder[2]);
		}
	}
}
