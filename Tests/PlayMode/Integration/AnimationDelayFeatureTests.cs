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
		// ADMIT: AnimationDelayFeature.OpenDelayInSeconds must report 0 when no intro clip is assigned, or a
		// clipless presenter stalls its open transition for a phantom duration.
		// RCR: AnimationDelayFeature.cs OpenDelayInSeconds — change the null-clip branch from `0f` to `1f` → RED
		// (expected 0, was 1). The non-null branch of the same ternary is a separate mutation. 2026-08-02
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

			// Wait for presenter's open transition to complete
			yield return presenter.OpenTransitionTask.ToCoroutine();

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
			yield return presenter.OpenTransitionTask.ToCoroutine();

			// Act
			_service.CloseUi(typeof(TestAnimationDelayPresenter));
			yield return presenter.CloseTransitionTask.ToCoroutine();

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
			yield return presenter.OpenTransitionTask.ToCoroutine();

			// Act
			_service.CloseUi(typeof(TestAnimationDelayPresenter));
			yield return presenter.CloseTransitionTask.ToCoroutine();

			// Assert
			Assert.IsFalse(presenter.IsOpen);
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
		// ADMIT: AnimationDelayFeature.OpenDelayInSeconds must derive the delay from the intro clip's length, or the
		// presenter stops waiting before the animation has played out.
		// RCR: AnimationDelayFeature.cs OpenDelayInSeconds — change the non-null branch from
		// `_introAnimationClip.length` to `0f` → RED (expected 0.1, was 0). Pins the other half of the ternary. 2026-08-02
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

		[UnityTest]
		public IEnumerator AnimationDelayFeature_ImplementsITransitionFeature()
		{
			// Act
			var task = _service.LoadUiAsync(typeof(TestAnimationDelayPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestAnimationDelayPresenter;

			// Assert
			Assert.IsTrue(presenter.AnimationFeature is ITransitionFeature);
			
			var transitionFeature = presenter.AnimationFeature as ITransitionFeature;
			Assert.IsNotNull(transitionFeature.OpenTransitionTask);
			Assert.IsNotNull(transitionFeature.CloseTransitionTask);
		}

		[UnityTest]
		// ADMIT: AnimationDelayFeature.CloseWithAnimationAsync must call OnCloseStarted before awaiting the outro —
		// that hook is what assigns the outro clip and plays it.
		// RCR: AnimationDelayFeature.cs CloseWithAnimationAsync — comment out `OnCloseStarted();` → RED (3 recorded
		// hooks, expected 4; CollectionAssert reports the missing "OnCloseStarted"). 2026-08-02
		public IEnumerator AnimationDelayFeature_LifecycleHooks_FireInOrderForOpenAndClose()
		{
			_mockLoader.RegisterPrefab<TestTrackingAnimationDelayPresenter>("tracking_animation_presenter");
			_service.AddUiConfig(TestHelpers.CreateTestConfig(typeof(TestTrackingAnimationDelayPresenter), "tracking_animation_presenter", 0));

			var openTask = _service.OpenUiAsync(typeof(TestTrackingAnimationDelayPresenter));
			yield return openTask.ToCoroutine();
			var presenter = openTask.GetAwaiter().GetResult() as TestTrackingAnimationDelayPresenter;
			yield return presenter.OpenTransitionTask.ToCoroutine();

			_service.CloseUi(typeof(TestTrackingAnimationDelayPresenter));
			yield return presenter.CloseTransitionTask.ToCoroutine();

			var hooks = presenter.AnimationFeature.RecordedHookOrder;
			Assert.AreEqual(4, hooks.Count, "Expected all 4 lifecycle hooks to fire across open + close");
			CollectionAssert.AreEqual(
				new[] { "OnOpenStarted", "OnOpenedCompleted", "OnCloseStarted", "OnClosedCompleted" },
				hooks,
				"Open hooks must fire in order before close hooks; close hooks must fire after CloseTransitionTask completes");
		}

		[UnityTest]
		// ADMIT: AnimationDelayFeature.OnOpenStarted assigns _animation.clip and calls _animation.Play(); the 8
		// tests above assert only derived delay timing and hook order, so deleting .Play() leaves them all green
		// while the intro animation silently never advances on screen.
		// RCR: AnimationDelayFeature.cs OnOpenStarted — comment out `_animation.Play();` → RED
		// (animation.isPlaying was False). 2026-08-01
		public IEnumerator OnOpen_WithClip_ActuallyPlaysTheAnimation()
		{
			// Arrange
			_mockLoader.RegisterPrefab<TestAnimationDelayWithClipPresenter>("animation_clip_presenter");
			_service.AddUiConfig(TestHelpers.CreateTestConfig(typeof(TestAnimationDelayWithClipPresenter), "animation_clip_presenter", 0));

			// Act
			var task = _service.OpenUiAsync(typeof(TestAnimationDelayWithClipPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestAnimationDelayWithClipPresenter;

			var animation = presenter.AnimationFeature.AnimationComponent;
			var clipName = presenter.AnimationFeature.IntroAnimationClip.name;

			// Assert - Play() was actually invoked (not merely the clip assigned)
			Assert.IsTrue(animation.isPlaying);

			var normalizedTimeRightAfterPlay = animation[clipName].normalizedTime;

			yield return null;

			// Assert - real playback time advances after a frame, proving it is actually running
			Assert.Greater(animation[clipName].normalizedTime, normalizedTimeRightAfterPlay);
		}
	}
}
