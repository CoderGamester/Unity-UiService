using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameLovers.UiService.Tests.PlayMode
{
	/// <summary>
	/// Tests for presenter features (TimeDelayFeature, AnimationDelayFeature) and transition lifecycle hooks.
	/// </summary>
	[TestFixture]
	public class PresenterFeatureTests
	{
		private MockAssetLoader _mockLoader;
		private UiService _service;

		[SetUp]
		public void Setup()
		{
			_mockLoader = new MockAssetLoader();
			_mockLoader.RegisterPrefab<TestPresenterWithFeature>("feature_presenter");
			_mockLoader.RegisterPrefab<TestPresenterWithTransitionFeature>("transition_feature_presenter");
			
			_service = new UiService(_mockLoader);
			
			var configs = TestHelpers.CreateTestConfigs(
				TestHelpers.CreateTestConfig(typeof(TestPresenterWithFeature), "feature_presenter", 0),
				TestHelpers.CreateTestConfig(typeof(TestPresenterWithTransitionFeature), "transition_feature_presenter", 0)
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
		public IEnumerator Feature_OnPresenterInitialized_CalledOnLoad()
		{
			// Act
			var task = _service.LoadUiAsync(typeof(TestPresenterWithFeature));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestPresenterWithFeature;

			// Assert
			Assert.IsNotNull(presenter);
			Assert.IsTrue(presenter.Feature.WasInitialized);
			Assert.AreEqual(presenter, presenter.Feature.ReceivedPresenter);
		}

		[UnityTest]
		public IEnumerator Feature_OnPresenterOpening_CalledBeforeOpen()
		{
			// Act
			var task = _service.OpenUiAsync(typeof(TestPresenterWithFeature));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestPresenterWithFeature;

			// Assert
			Assert.IsTrue(presenter.Feature.WasOpening);
		}

		[UnityTest]
		public IEnumerator Feature_OnPresenterOpened_CalledAfterOpen()
		{
			// Act
			var task = _service.OpenUiAsync(typeof(TestPresenterWithFeature));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestPresenterWithFeature;

			// Assert
			Assert.IsTrue(presenter.Feature.WasOpened);
		}

		[UnityTest]
		public IEnumerator Feature_OnPresenterClosing_CalledOnClose()
		{
			// Arrange
			var task = _service.OpenUiAsync(typeof(TestPresenterWithFeature));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestPresenterWithFeature;

			// Act
			_service.CloseUi(typeof(TestPresenterWithFeature));

			// Assert
			Assert.IsTrue(presenter.Feature.WasClosing);
		}

		[UnityTest]
		public IEnumerator Feature_OnPresenterClosed_CalledAfterClose()
		{
			// Arrange
			var task = _service.OpenUiAsync(typeof(TestPresenterWithFeature));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestPresenterWithFeature;

			// Act
			_service.CloseUi(typeof(TestPresenterWithFeature));

			// Assert
			Assert.IsTrue(presenter.Feature.WasClosed);
		}

		[UnityTest]
		public IEnumerator OnOpenTransitionCompleted_AlwaysCalledForPresentersWithoutFeatures()
		{
			// Arrange - Using presenter with non-transition feature (no ITransitionFeature)
			var task = _service.OpenUiAsync(typeof(TestPresenterWithFeature));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestPresenterWithFeature;

			// Wait a frame for async process to complete
			yield return null;

			// Assert - OnOpenTransitionCompleted should always be called
			Assert.IsTrue(presenter.WasOpenTransitionCompleted);
			Assert.AreEqual(1, presenter.OpenTransitionCompletedCount);
		}

		[UnityTest]
		public IEnumerator OnCloseTransitionCompleted_AlwaysCalledForPresentersWithoutFeatures()
		{
			// Arrange
			var task = _service.OpenUiAsync(typeof(TestPresenterWithFeature));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestPresenterWithFeature;

			// Wait for open transition to complete
			yield return null;

			// Act
			_service.CloseUi(typeof(TestPresenterWithFeature));
			
			// Wait for close transition to complete
			yield return null;

			// Assert - OnCloseTransitionCompleted should always be called
			Assert.IsTrue(presenter.WasCloseTransitionCompleted);
			Assert.AreEqual(1, presenter.CloseTransitionCompletedCount);
		}

		[UnityTest]
		public IEnumerator TransitionFeature_PresenterAwaitsOpenTransition()
		{
			// Arrange
			var task = _service.OpenUiAsync(typeof(TestPresenterWithTransitionFeature));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestPresenterWithTransitionFeature;

			// Transition not yet complete
			Assert.IsFalse(presenter.WasOpenTransitionCompleted);

			// Act - Complete the transition
			presenter.TransitionFeature.CompleteOpenTransition();
			
			// Wait for presenter to process
			yield return null;

			// Assert
			Assert.IsTrue(presenter.WasOpenTransitionCompleted);
		}

		[UnityTest]
		public IEnumerator TransitionFeature_PresenterAwaitsCloseTransition()
		{
			// Arrange
			var task = _service.OpenUiAsync(typeof(TestPresenterWithTransitionFeature));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestPresenterWithTransitionFeature;
			
			// Complete open transition first
			presenter.TransitionFeature.CompleteOpenTransition();
			yield return null;

			// Act - Close and verify transition is awaited
			_service.CloseUi(typeof(TestPresenterWithTransitionFeature));
			yield return null;

			// Close transition not yet complete
			Assert.IsFalse(presenter.WasCloseTransitionCompleted);
			Assert.IsTrue(presenter.gameObject.activeSelf); // Still visible during transition

			// Complete the transition
			presenter.TransitionFeature.CompleteCloseTransition();
			yield return null;

			// Assert
			Assert.IsTrue(presenter.WasCloseTransitionCompleted);
			Assert.IsFalse(presenter.gameObject.activeSelf); // Hidden after transition
		}

		[UnityTest]
		public IEnumerator TransitionFeature_GameObjectHiddenOnlyAfterTransitionCompletes()
		{
			// Arrange
			var task = _service.OpenUiAsync(typeof(TestPresenterWithTransitionFeature));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestPresenterWithTransitionFeature;
			presenter.TransitionFeature.CompleteOpenTransition();
			yield return null;

			// Act - Start close
			_service.CloseUi(typeof(TestPresenterWithTransitionFeature));
			yield return null;

			// Assert - Still visible during transition
			Assert.IsTrue(presenter.gameObject.activeSelf);

			// Complete transition
			presenter.TransitionFeature.CompleteCloseTransition();
			yield return null;

			// Assert - Now hidden
			Assert.IsFalse(presenter.gameObject.activeSelf);
		}
	}

	/// <summary>
	/// Test presenter with a mock feature (non-transition) for testing basic feature lifecycle
	/// </summary>
	[RequireComponent(typeof(MockPresenterFeature))]
	public class TestPresenterWithFeature : UiPresenter
	{
		public MockPresenterFeature Feature { get; private set; }
		public bool WasOpenTransitionCompleted { get; private set; }
		public bool WasCloseTransitionCompleted { get; private set; }
		public int OpenTransitionCompletedCount { get; private set; }
		public int CloseTransitionCompletedCount { get; private set; }

		private void Awake()
		{
			Feature = GetComponent<MockPresenterFeature>();
			if (Feature == null)
			{
				Feature = gameObject.AddComponent<MockPresenterFeature>();
			}
		}

		protected override void OnOpenTransitionCompleted()
		{
			WasOpenTransitionCompleted = true;
			OpenTransitionCompletedCount++;
		}

		protected override void OnCloseTransitionCompleted()
		{
			WasCloseTransitionCompleted = true;
			CloseTransitionCompletedCount++;
		}
	}

	/// <summary>
	/// Test presenter with a mock transition feature for testing ITransitionFeature
	/// </summary>
	[RequireComponent(typeof(MockTransitionFeature))]
	public class TestPresenterWithTransitionFeature : UiPresenter
	{
		public MockTransitionFeature TransitionFeature { get; private set; }
		public bool WasOpenTransitionCompleted { get; private set; }
		public bool WasCloseTransitionCompleted { get; private set; }

		private void Awake()
		{
			TransitionFeature = GetComponent<MockTransitionFeature>();
			if (TransitionFeature == null)
			{
				TransitionFeature = gameObject.AddComponent<MockTransitionFeature>();
			}
		}

		protected override void OnOpenTransitionCompleted()
		{
			WasOpenTransitionCompleted = true;
		}

		protected override void OnCloseTransitionCompleted()
		{
			WasCloseTransitionCompleted = true;
		}
	}

	/// <summary>
	/// Mock feature for testing basic feature lifecycle (does not implement ITransitionFeature)
	/// </summary>
	public class MockPresenterFeature : PresenterFeatureBase
	{
		public bool WasInitialized { get; private set; }
		public bool WasOpening { get; private set; }
		public bool WasOpened { get; private set; }
		public bool WasClosing { get; private set; }
		public bool WasClosed { get; private set; }
		public UiPresenter ReceivedPresenter { get; private set; }

		public override void OnPresenterInitialized(UiPresenter presenter)
		{
			base.OnPresenterInitialized(presenter);
			WasInitialized = true;
			ReceivedPresenter = presenter;
		}

		public override void OnPresenterOpening()
		{
			WasOpening = true;
		}

		public override void OnPresenterOpened()
		{
			WasOpened = true;
		}

		public override void OnPresenterClosing()
		{
			WasClosing = true;
		}

		public override void OnPresenterClosed()
		{
			WasClosed = true;
		}
	}

	/// <summary>
	/// Mock feature that implements ITransitionFeature for testing transition awaiting
	/// </summary>
	public class MockTransitionFeature : PresenterFeatureBase, ITransitionFeature
	{
		private UniTaskCompletionSource _openTransitionCompletion;
		private UniTaskCompletionSource _closeTransitionCompletion;

		public UniTask OpenTransitionTask => _openTransitionCompletion?.Task ?? UniTask.CompletedTask;
		public UniTask CloseTransitionTask => _closeTransitionCompletion?.Task ?? UniTask.CompletedTask;

		public override void OnPresenterOpened()
		{
			_openTransitionCompletion = new UniTaskCompletionSource();
		}

		public override void OnPresenterClosing()
		{
			_closeTransitionCompletion = new UniTaskCompletionSource();
		}

		public void CompleteOpenTransition()
		{
			_openTransitionCompletion?.TrySetResult();
		}

		public void CompleteCloseTransition()
		{
			_closeTransitionCompletion?.TrySetResult();
		}
	}
}
