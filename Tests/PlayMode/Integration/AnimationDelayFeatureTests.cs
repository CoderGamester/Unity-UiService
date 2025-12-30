using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace GameLovers.UiService.Tests.PlayMode
{
	/// <summary>
	/// Tests for AnimationDelayFeature functionality.
	/// </summary>
	[TestFixture]
	public class AnimationDelayFeatureTests
	{
		private MockAssetLoader _mockLoader;
		private UiService _service;

		[SetUp]
		public void Setup()
		{
			_mockLoader = new MockAssetLoader();
			_mockLoader.RegisterPrefab<TestAnimationDelayPresenter>("animation_presenter");
			
			_service = new UiService(_mockLoader);
			
			var configs = TestHelpers.CreateTestConfigs(
				TestHelpers.CreateTestConfig(typeof(TestAnimationDelayPresenter), "animation_presenter", 0)
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
		public IEnumerator AnimationDelayFeature_NoClips_HasZeroDelay()
		{
			// Act
			var task = _service.LoadUiAsync(typeof(TestAnimationDelayPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestAnimationDelayPresenter;

			// Assert - No clips means zero delay
			Assert.AreEqual(0f, presenter.AnimationFeature.OpenDelayInSeconds);
			Assert.AreEqual(0f, presenter.AnimationFeature.CloseDelayInSeconds);
		}

		[UnityTest]
		public IEnumerator AnimationDelayFeature_OnOpen_NotifiesTransitionCompleted()
		{
			// Act
			var task = _service.OpenUiAsync(typeof(TestAnimationDelayPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestAnimationDelayPresenter;

			// Wait for animation to complete (or immediate if no clip)
			yield return presenter.AnimationFeature.CurrentDelayTask.ToCoroutine();
			yield return null;

			// Assert
			Assert.IsTrue(presenter.WasOpenTransitionCompleted);
		}

		[UnityTest]
		public IEnumerator AnimationDelayFeature_OnClose_NotifiesTransitionCompleted()
		{
			// Arrange
			var task = _service.OpenUiAsync(typeof(TestAnimationDelayPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestAnimationDelayPresenter;
			yield return presenter.AnimationFeature.CurrentDelayTask.ToCoroutine();

			// Act
			_service.CloseUi(typeof(TestAnimationDelayPresenter));
			yield return presenter.AnimationFeature.CurrentDelayTask.ToCoroutine();
			yield return null;

			// Assert
			Assert.IsTrue(presenter.WasCloseTransitionCompleted);
		}

		[UnityTest]
		public IEnumerator AnimationDelayFeature_OnClose_DeactivatesGameObject()
		{
			// Arrange
			var task = _service.OpenUiAsync(typeof(TestAnimationDelayPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestAnimationDelayPresenter;
			yield return presenter.AnimationFeature.CurrentDelayTask.ToCoroutine();

			// Act
			_service.CloseUi(typeof(TestAnimationDelayPresenter));
			yield return presenter.AnimationFeature.CurrentDelayTask.ToCoroutine();
			yield return null;

			// Assert
			Assert.IsFalse(presenter.gameObject.activeSelf);
		}

		[UnityTest]
		public IEnumerator AnimationDelayFeature_AnimationComponent_IsAssigned()
		{
			// Act
			var task = _service.LoadUiAsync(typeof(TestAnimationDelayPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestAnimationDelayPresenter;

			// Assert
			Assert.IsNotNull(presenter.AnimationFeature.AnimationComponent);
		}

		[UnityTest]
		public IEnumerator AnimationDelayFeature_WithClip_UsesClipLength()
		{
			// Arrange - Create a test animation clip
			_mockLoader.RegisterPrefab<TestAnimationDelayWithClipPresenter>("animation_clip_presenter");
			_service.AddUiConfig(TestHelpers.CreateTestConfig(typeof(TestAnimationDelayWithClipPresenter), "animation_clip_presenter", 0));

			// Act
			var task = _service.LoadUiAsync(typeof(TestAnimationDelayWithClipPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestAnimationDelayWithClipPresenter;

			// Assert - Should use clip length
			Assert.AreEqual(0.1f, presenter.AnimationFeature.OpenDelayInSeconds, 0.001f);
		}
	}
}
