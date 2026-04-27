using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameLovers.UiService.Tests.PlayMode
{
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

	public class SelfClosingPresenter : UiPresenter
	{
		public void RequestSelfClose(bool destroy) => Close(destroy);
	}

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

		public override void OnPresenterOpening() => WasOpening = true;
		public override void OnPresenterOpened() => WasOpened = true;
		public override void OnPresenterClosing() => WasClosing = true;
		public override void OnPresenterClosed() => WasClosed = true;
	}

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

		public void CompleteOpenTransition() => _openTransitionCompletion?.TrySetResult();
		public void CompleteCloseTransition() => _closeTransitionCompletion?.TrySetResult();
	}

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
			var task = _service.LoadUiAsync(typeof(TestPresenterWithFeature));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestPresenterWithFeature;

			Assert.IsNotNull(presenter);
			Assert.IsTrue(presenter.Feature.WasInitialized);
			Assert.AreEqual(presenter, presenter.Feature.ReceivedPresenter);
		}

		[UnityTest]
		public IEnumerator Feature_OnPresenterOpening_CalledBeforeOpen()
		{
			var task = _service.OpenUiAsync(typeof(TestPresenterWithFeature));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestPresenterWithFeature;

			Assert.IsTrue(presenter.Feature.WasOpening);
		}

		[UnityTest]
		public IEnumerator Feature_OnPresenterOpened_CalledAfterOpen()
		{
			var task = _service.OpenUiAsync(typeof(TestPresenterWithFeature));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestPresenterWithFeature;

			Assert.IsTrue(presenter.Feature.WasOpened);
		}

		[UnityTest]
		public IEnumerator Feature_OnPresenterClosing_CalledOnClose()
		{
			var task = _service.OpenUiAsync(typeof(TestPresenterWithFeature));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestPresenterWithFeature;

			_service.CloseUi(typeof(TestPresenterWithFeature));

			Assert.IsTrue(presenter.Feature.WasClosing);
		}

		[UnityTest]
		public IEnumerator Feature_OnPresenterClosed_CalledAfterClose()
		{
			var task = _service.OpenUiAsync(typeof(TestPresenterWithFeature));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestPresenterWithFeature;

			_service.CloseUi(typeof(TestPresenterWithFeature));

			Assert.IsTrue(presenter.Feature.WasClosed);
		}

		[UnityTest]
		public IEnumerator OnOpenTransitionCompleted_AlwaysCalledForPresentersWithoutFeatures()
		{
			var task = _service.OpenUiAsync(typeof(TestPresenterWithFeature));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestPresenterWithFeature;

			yield return null;

			Assert.IsTrue(presenter.WasOpenTransitionCompleted);
			Assert.AreEqual(1, presenter.OpenTransitionCompletedCount);
		}

		[UnityTest]
		public IEnumerator OnCloseTransitionCompleted_AlwaysCalledForPresentersWithoutFeatures()
		{
			var task = _service.OpenUiAsync(typeof(TestPresenterWithFeature));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestPresenterWithFeature;

			yield return null;

			_service.CloseUi(typeof(TestPresenterWithFeature));
			yield return null;

			Assert.IsTrue(presenter.WasCloseTransitionCompleted);
			Assert.AreEqual(1, presenter.CloseTransitionCompletedCount);
		}

		[UnityTest]
		public IEnumerator TransitionFeature_PresenterAwaitsOpenTransition()
		{
			var task = _service.OpenUiAsync(typeof(TestPresenterWithTransitionFeature));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestPresenterWithTransitionFeature;

			Assert.IsFalse(presenter.WasOpenTransitionCompleted);

			presenter.TransitionFeature.CompleteOpenTransition();
			yield return null;

			Assert.IsTrue(presenter.WasOpenTransitionCompleted);
		}

		[UnityTest]
		public IEnumerator TransitionFeature_PresenterAwaitsCloseTransition()
		{
			var task = _service.OpenUiAsync(typeof(TestPresenterWithTransitionFeature));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestPresenterWithTransitionFeature;

			presenter.TransitionFeature.CompleteOpenTransition();
			yield return null;

			_service.CloseUi(typeof(TestPresenterWithTransitionFeature));
			yield return null;

			Assert.IsFalse(presenter.WasCloseTransitionCompleted);
			Assert.IsTrue(presenter.IsOpen);

			presenter.TransitionFeature.CompleteCloseTransition();
			yield return null;

			Assert.IsTrue(presenter.WasCloseTransitionCompleted);
			Assert.IsFalse(presenter.IsOpen);
		}

		[UnityTest]
		public IEnumerator TransitionFeature_GameObjectHiddenOnlyAfterTransitionCompletes()
		{
			var task = _service.OpenUiAsync(typeof(TestPresenterWithTransitionFeature));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestPresenterWithTransitionFeature;
			presenter.TransitionFeature.CompleteOpenTransition();
			yield return null;

			_service.CloseUi(typeof(TestPresenterWithTransitionFeature));
			yield return null;

			Assert.IsTrue(presenter.IsOpen);

			presenter.TransitionFeature.CompleteCloseTransition();
			yield return null;

			Assert.IsFalse(presenter.IsOpen);
		}

		[UnityTest]
		public IEnumerator Close_FromInsidePresenter_RoutesThroughIUiService()
		{
			_mockLoader.RegisterPrefab<SelfClosingPresenter>("self_closing");
			_service.AddUiConfig(TestHelpers.CreateTestConfig(typeof(SelfClosingPresenter), "self_closing", 0));

			var openTask = _service.OpenUiAsync(typeof(SelfClosingPresenter));
			yield return openTask.ToCoroutine();
			var presenter = openTask.GetAwaiter().GetResult() as SelfClosingPresenter;
			yield return presenter.OpenTransitionTask.ToCoroutine();

			Assert.That(_service.VisiblePresenters.Count, Is.EqualTo(1));

			presenter.RequestSelfClose(destroy: false);
			yield return presenter.CloseTransitionTask.ToCoroutine();

			Assert.That(_service.VisiblePresenters.Count, Is.EqualTo(0));
			Assert.That(presenter.IsOpen, Is.False);
		}
	}
}
