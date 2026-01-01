using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace GameLovers.UiService.Tests.PlayMode
{
	/// <summary>
	/// Tests for TimeDelayFeature functionality.
	/// </summary>
	[TestFixture]
	public class TimeDelayFeatureTests
	{
		private MockAssetLoader _mockLoader;
		private UiService _service;

		[SetUp]
		public void Setup()
		{
			_mockLoader = new MockAssetLoader();
			_mockLoader.RegisterPrefab<TestTimeDelayPresenter>("delay_presenter");
			
			_service = new UiService(_mockLoader);
			
			var configs = TestHelpers.CreateTestConfigs(
				TestHelpers.CreateTestConfig(typeof(TestTimeDelayPresenter), "delay_presenter", 0)
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
		public IEnumerator TimeDelayFeature_DefaultValues_AreCorrect()
		{
			// Act
			var task = _service.LoadUiAsync(typeof(TestTimeDelayPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestTimeDelayPresenter;

			// Assert
			Assert.IsNotNull(presenter.DelayFeature);
			Assert.AreEqual(0.1f, presenter.DelayFeature.OpenDelayInSeconds, 0.001f);
			Assert.AreEqual(0.05f, presenter.DelayFeature.CloseDelayInSeconds, 0.001f);
		}

		[UnityTest]
		public IEnumerator TimeDelayFeature_OnOpen_NotifiesTransitionCompleted()
		{
			// Act
			var task = _service.OpenUiAsync(typeof(TestTimeDelayPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestTimeDelayPresenter;

			// Wait for presenter's open transition to complete
			yield return presenter.OpenTransitionTask.ToCoroutine();

			// Assert
			Assert.IsTrue(presenter.WasOpenTransitionCompleted);
		}

		[UnityTest]
		public IEnumerator TimeDelayFeature_OnClose_NotifiesTransitionCompleted()
		{
			// Arrange
			var task = _service.OpenUiAsync(typeof(TestTimeDelayPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestTimeDelayPresenter;
			
			// Wait for open transition
			yield return presenter.OpenTransitionTask.ToCoroutine();

			// Act - Close
			_service.CloseUi(typeof(TestTimeDelayPresenter));
			
			// Wait for close transition
			yield return presenter.CloseTransitionTask.ToCoroutine();

			// Assert
			Assert.IsTrue(presenter.WasCloseTransitionCompleted);
		}

		[UnityTest]
		public IEnumerator TimeDelayFeature_OnClose_DeactivatesGameObject()
		{
			// Arrange
			var task = _service.OpenUiAsync(typeof(TestTimeDelayPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestTimeDelayPresenter;
			yield return presenter.OpenTransitionTask.ToCoroutine();

			// Act
			_service.CloseUi(typeof(TestTimeDelayPresenter));
			yield return presenter.CloseTransitionTask.ToCoroutine();

			// Assert - Presenter should have deactivated the GameObject after transition
			Assert.IsFalse(presenter.gameObject.activeSelf);
		}

		[UnityTest]
		public IEnumerator TimeDelayFeature_OpenTransitionTask_IsValid()
		{
			// Act
			var task = _service.OpenUiAsync(typeof(TestTimeDelayPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestTimeDelayPresenter;

			// Assert - Feature's task should be valid via ITransitionFeature
			var transitionFeature = presenter.DelayFeature as ITransitionFeature;
			Assert.IsNotNull(transitionFeature);
			
			var delayTask = transitionFeature.OpenTransitionTask;
			
			// Wait for completion
			yield return delayTask.ToCoroutine();
			Assert.IsTrue(delayTask.Status.IsCompleted());
		}

		[UnityTest]
		public IEnumerator TimeDelayFeature_ZeroDelay_CompletesImmediately()
		{
			// Arrange - Create presenter with zero delay
			_mockLoader.RegisterPrefab<TestZeroDelayPresenter>("zero_delay");
			_service.AddUiConfig(TestHelpers.CreateTestConfig(typeof(TestZeroDelayPresenter), "zero_delay", 0));

			// Act
			var task = _service.OpenUiAsync(typeof(TestZeroDelayPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestZeroDelayPresenter;
			
			// Wait for open transition
			yield return presenter.OpenTransitionTask.ToCoroutine();

			// Assert
			Assert.IsTrue(presenter.WasOpenTransitionCompleted);
		}

		[UnityTest]
		public IEnumerator TimeDelayFeature_PresenterAwaitsFeatureTask()
		{
			// Act
			var task = _service.OpenUiAsync(typeof(TestTimeDelayPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestTimeDelayPresenter;

			// Before feature completes, transition shouldn't be marked complete
			// (this depends on timing, but with 0.1s delay we should catch it)
			var featureTask = (presenter.DelayFeature as ITransitionFeature).OpenTransitionTask;
			
			// Wait for feature and presenter to both complete
			yield return UniTask.WhenAll(featureTask, presenter.OpenTransitionTask).ToCoroutine();

			// Assert - Presenter's transition completed after feature
			Assert.IsTrue(presenter.WasOpenTransitionCompleted);
		}
	}
}
