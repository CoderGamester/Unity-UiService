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
			
			_service = new UiService(_mockLoader);
			
			var configs = TestHelpers.CreateTestConfigs(
				TestHelpers.CreateTestConfig(typeof(TestPresenterWithFeature), "feature_presenter", 0)
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
		public IEnumerator NotifyOpenTransitionCompleted_TriggersPresenterHook()
		{
			// Arrange
			var task = _service.OpenUiAsync(typeof(TestPresenterWithFeature));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestPresenterWithFeature;

			// Act - Feature calls NotifyOpenTransitionCompleted
			presenter.Feature.SimulateOpenTransitionComplete();

			// Assert
			Assert.IsTrue(presenter.WasOpenTransitionCompleted);
			Assert.AreEqual(1, presenter.OpenTransitionCompletedCount);
		}

		[UnityTest]
		public IEnumerator NotifyCloseTransitionCompleted_TriggersPresenterHook()
		{
			// Arrange
			var task = _service.OpenUiAsync(typeof(TestPresenterWithFeature));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestPresenterWithFeature;
			_service.CloseUi(typeof(TestPresenterWithFeature));

			// Act - Feature calls NotifyCloseTransitionCompleted
			presenter.Feature.SimulateCloseTransitionComplete();

			// Assert
			Assert.IsTrue(presenter.WasCloseTransitionCompleted);
			Assert.AreEqual(1, presenter.CloseTransitionCompletedCount);
		}

		[UnityTest]
		public IEnumerator MultipleTransitionNotifications_IncrementCount()
		{
			// Arrange
			var task = _service.OpenUiAsync(typeof(TestPresenterWithFeature));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestPresenterWithFeature;

			// Act
			presenter.Feature.SimulateOpenTransitionComplete();
			presenter.Feature.SimulateOpenTransitionComplete();
			presenter.Feature.SimulateOpenTransitionComplete();

			// Assert
			Assert.AreEqual(3, presenter.OpenTransitionCompletedCount);
		}
	}

	/// <summary>
	/// Test presenter with a mock feature for testing feature lifecycle
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
	/// Mock feature for testing feature lifecycle
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

		/// <summary>
		/// Simulates a feature completing its open transition
		/// </summary>
		public void SimulateOpenTransitionComplete()
		{
			Presenter.NotifyOpenTransitionCompleted();
		}

		/// <summary>
		/// Simulates a feature completing its close transition
		/// </summary>
		public void SimulateCloseTransitionComplete()
		{
			Presenter.NotifyCloseTransitionCompleted();
		}
	}
}

