using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameLovers.UiService.Tests.PlayMode
{
	[TestFixture]
	public class UiServiceLoadingTests
	{
		private MockAssetLoader _mockLoader;
		private UiService _service;

		[SetUp]
		public void Setup()
		{
			_mockLoader = new MockAssetLoader();
			_mockLoader.RegisterPrefab<TestUiPresenter>("test_presenter");
			_mockLoader.RegisterPrefab<TestDataUiPresenter>("data_presenter");
			
			_service = new UiService(_mockLoader);
			
			var configs = TestHelpers.CreateTestConfigs(
				TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "test_presenter", 0),
				TestHelpers.CreateTestConfig(typeof(TestDataUiPresenter), "data_presenter", 1)
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
		public IEnumerator LoadUiAsync_ValidConfig_LoadsPresenter()
		{
			// Act
			var task = _service.LoadUiAsync(typeof(TestUiPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult();

			// Assert
			Assert.IsNotNull(presenter);
			Assert.IsInstanceOf<TestUiPresenter>(presenter);
			Assert.That(presenter.IsOpen, Is.False);
		}

		[UnityTest]
		public IEnumerator LoadUiAsync_WithOpenAfter_OpensPresenter()
		{
			// Act
			var task = _service.LoadUiAsync(typeof(TestUiPresenter), openAfter: true);
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult();

			// Assert
			Assert.IsNotNull(presenter);
			Assert.That(presenter.IsOpen, Is.True);
			Assert.That(_service.VisiblePresenters.Count, Is.EqualTo(1));
		}
	
		[UnityTest]
		public IEnumerator LoadUiAsync_AlreadyLoaded_ReturnsExisting()
		{
			// Arrange
			var task1 = _service.LoadUiAsync(typeof(TestUiPresenter));
			yield return task1.ToCoroutine();
			var firstPresenter = task1.GetAwaiter().GetResult();

			// Expected: second load hits the "already loaded" warning by design
			LogAssert.Expect(LogType.Warning, new Regex("was already loaded"));

			// Act
			var task2 = _service.LoadUiAsync(typeof(TestUiPresenter));
			yield return task2.ToCoroutine();
			var secondPresenter = task2.GetAwaiter().GetResult();

			// Assert
			Assert.AreEqual(firstPresenter, secondPresenter);
			Assert.AreEqual(1, _mockLoader.InstantiateCallCount);
		}
	
		[UnityTest]
		public IEnumerator LoadUiAsync_MissingConfig_ThrowsException()
		{
			// Arrange - Remove configs and recreate service with no TestUiPresenter config
			_service.Dispose();
			_service = new UiService(_mockLoader);
			_service.Init(TestHelpers.CreateTestConfigs());

			// Act & Assert
			KeyNotFoundException caughtException = null;
			var task = _service.LoadUiAsync(typeof(TestUiPresenter));
			
			while (!task.Status.IsCompleted())
			{
				yield return null;
			}
			
			try
			{
				task.GetAwaiter().GetResult();
			}
			catch (KeyNotFoundException ex)
			{
				caughtException = ex;
			}

			Assert.IsNotNull(caughtException);
		}
	
		[UnityTest]
		public IEnumerator UnloadUi_LoadedUi_UnloadsCorrectly()
		{
			// Arrange
			var task = _service.LoadUiAsync(typeof(TestUiPresenter));
			yield return task.ToCoroutine();

			// Act
			_service.UnloadUi(typeof(TestUiPresenter));

			// Assert
			Assert.AreEqual(1, _mockLoader.UnloadCallCount);
			Assert.AreEqual(0, _service.GetLoadedPresenters().Count);
		}
	
		[UnityTest]
		public IEnumerator LoadUiAsync_WithCancellation_CancelsOperation()
		{
			// Arrange
			_mockLoader.SimulatedDelayMs = 1000;
			var cts = new CancellationTokenSource();

			// Act
			var task = _service.LoadUiAsync(typeof(TestUiPresenter), false, cts.Token);
			yield return null; // Let it start
			cts.Cancel();

			// Wait for the task to complete (cancelled or otherwise)
			while (!task.Status.IsCompleted())
			{
				yield return null;
			}

			// Assert
			System.OperationCanceledException caughtException = null;
			try
			{
				task.GetAwaiter().GetResult();
			}
			catch (System.OperationCanceledException ex)
			{
				caughtException = ex;
			}
			
			Assert.IsNotNull(caughtException);
		}

		#region Visibility State Consistency Tests

		/// <summary>
		/// Verifies that LoadUiAsync on an already-visible presenter does NOT change visibility.
		/// LoadUiAsync is a load operation, not a visibility management operation.
		/// The openAfter parameter only applies when the presenter is newly loaded.
		/// </summary>
		[UnityTest]
		public IEnumerator LoadUiAsync_OnVisiblePresenter_WithOpenAfterFalse_DoesNotChangeVisibility()
		{
			// Arrange - Open the presenter first
			var openTask = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return openTask.ToCoroutine();
			var presenter = openTask.GetAwaiter().GetResult();
			yield return presenter.OpenTransitionTask.ToCoroutine();

			Assert.That(_service.VisiblePresenters.Count, Is.EqualTo(1));
			Assert.That(presenter.IsOpen, Is.True);

			// Expected: LoadUiAsync on an already-loaded presenter logs a warning by design
			LogAssert.Expect(LogType.Warning, new Regex("was already loaded"));

			// Act - Call LoadUiAsync with openAfter=false on already visible presenter
			var loadTask = _service.LoadUiAsync(typeof(TestUiPresenter), openAfter: false);
			yield return loadTask.ToCoroutine();

			// Assert - Presenter should remain visible (LoadUiAsync doesn't manage visibility of already-loaded presenters)
			Assert.That(_service.VisiblePresenters.Count, Is.EqualTo(1));
			Assert.That(presenter.IsOpen, Is.True);
		}

		/// <summary>
		/// Verifies that LoadUiAsync on an already-visible presenter with openAfter=true keeps it visible.
		/// </summary>
		[UnityTest]
		public IEnumerator LoadUiAsync_OnVisiblePresenter_WithOpenAfterTrue_RemainsOpen()
		{
			// Arrange - Open the presenter first
			var openTask = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return openTask.ToCoroutine();
			var presenter = openTask.GetAwaiter().GetResult();
			yield return presenter.OpenTransitionTask.ToCoroutine();

			Assert.That(_service.VisiblePresenters.Count, Is.EqualTo(1));

			// Expected: LoadUiAsync on an already-loaded presenter logs a warning by design
			LogAssert.Expect(LogType.Warning, new Regex("was already loaded"));

			// Act - Call LoadUiAsync with openAfter=true on already visible presenter
			var loadTask = _service.LoadUiAsync(typeof(TestUiPresenter), openAfter: true);
			yield return loadTask.ToCoroutine();

			// Assert - Presenter should remain visible (no state change)
			Assert.That(_service.VisiblePresenters.Count, Is.EqualTo(1));
			Assert.That(presenter.IsOpen, Is.True);
		}

		/// <summary>
		/// Verifies that CloseUi properly closes a presenter and OpenUiAsync can reopen it.
		/// This tests the correct API for managing visibility (CloseUi, not LoadUiAsync).
		/// </summary>
		[UnityTest]
		public IEnumerator OpenUiAsync_AfterCloseUi_OpensSuccessfully()
		{
			// Arrange - Open the presenter, then close it via CloseUi
			var openTask1 = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return openTask1.ToCoroutine();
			var presenter = openTask1.GetAwaiter().GetResult();
			yield return presenter.OpenTransitionTask.ToCoroutine();

			// Close via CloseUi (the correct API for closing)
			_service.CloseUi(typeof(TestUiPresenter));
			yield return presenter.CloseTransitionTask.ToCoroutine();

			Assert.That(_service.VisiblePresenters.Count, Is.EqualTo(0));

			// Act - Open again
			var openTask2 = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return openTask2.ToCoroutine();
			var reopenedPresenter = openTask2.GetAwaiter().GetResult();
			yield return reopenedPresenter.OpenTransitionTask.ToCoroutine();

			// Assert - Presenter should open successfully
			Assert.That(_service.VisiblePresenters.Count, Is.EqualTo(1));
			Assert.That(reopenedPresenter.IsOpen, Is.True);
			Assert.AreEqual(presenter, reopenedPresenter); // Same instance
		}

		/// <summary>
		/// Verifies that CloseUi properly calls close lifecycle hooks.
		/// This tests the correct API for closing (CloseUi, not LoadUiAsync).
		/// </summary>
		[UnityTest]
		public IEnumerator CloseUi_OnVisiblePresenter_CallsCloseLifecycleHooks()
		{
			// Arrange - Open the presenter first
			var openTask = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return openTask.ToCoroutine();
			var presenter = openTask.GetAwaiter().GetResult() as TestUiPresenter;
			yield return presenter.OpenTransitionTask.ToCoroutine();

			var closeCountBefore = presenter.CloseCount;
			var closeTransitionCountBefore = presenter.CloseTransitionCompletedCount;

			// Act - Call CloseUi (the correct API for closing)
			_service.CloseUi(typeof(TestUiPresenter));
			yield return presenter.CloseTransitionTask.ToCoroutine();

			// Assert - Close lifecycle hooks should have been called
			Assert.That(presenter.CloseCount, Is.EqualTo(closeCountBefore + 1));
			Assert.That(presenter.CloseTransitionCompletedCount, Is.EqualTo(closeTransitionCountBefore + 1));
		}

		/// <summary>
		/// Verifies that LoadUiAsync on an already-loaded but closed presenter with openAfter=true
		/// does NOT open it (openAfter only applies to newly loaded presenters).
		/// Use OpenUiAsync to open an already-loaded presenter.
		/// </summary>
		[UnityTest]
		public IEnumerator LoadUiAsync_OnClosedPresenter_WithOpenAfterTrue_DoesNotOpen()
		{
			// Arrange - Load but don't open (presenter is closed)
			var loadTask1 = _service.LoadUiAsync(typeof(TestUiPresenter), openAfter: false);
			yield return loadTask1.ToCoroutine();
			var presenter = loadTask1.GetAwaiter().GetResult() as TestUiPresenter;

			Assert.That(_service.VisiblePresenters.Count, Is.EqualTo(0));
			var openCountBefore = presenter.OpenCount;

			// Expected: LoadUiAsync on an already-loaded (but closed) presenter logs a warning by design
			LogAssert.Expect(LogType.Warning, new Regex("was already loaded"));

			// Act - Call LoadUiAsync with openAfter=true on already-loaded closed presenter
			var loadTask2 = _service.LoadUiAsync(typeof(TestUiPresenter), openAfter: true);
			yield return loadTask2.ToCoroutine();

			// Assert - Presenter should remain closed (openAfter only applies to new loads)
			Assert.That(_service.VisiblePresenters.Count, Is.EqualTo(0));
			Assert.That(presenter.OpenCount, Is.EqualTo(openCountBefore));
		}

		/// <summary>
		/// Verifies that OpenUiAsync properly opens an already-loaded but closed presenter.
		/// This is the correct way to open an already-loaded presenter.
		/// </summary>
		[UnityTest]
		public IEnumerator OpenUiAsync_OnClosedPresenter_CallsOpenLifecycleHooks()
		{
			// Arrange - Load but don't open (presenter is closed)
			var loadTask = _service.LoadUiAsync(typeof(TestUiPresenter), openAfter: false);
			yield return loadTask.ToCoroutine();
			var presenter = loadTask.GetAwaiter().GetResult() as TestUiPresenter;

			Assert.That(_service.VisiblePresenters.Count, Is.EqualTo(0));
			var openCountBefore = presenter.OpenCount;
			var openTransitionCountBefore = presenter.OpenTransitionCompletedCount;

			// Act - Call OpenUiAsync (the correct API to open an already-loaded presenter)
			var openTask = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return openTask.ToCoroutine();
			yield return presenter.OpenTransitionTask.ToCoroutine();

			// Assert - Open lifecycle hooks should have been called
			Assert.That(_service.VisiblePresenters.Count, Is.EqualTo(1));
			Assert.That(presenter.OpenCount, Is.EqualTo(openCountBefore + 1));
			Assert.That(presenter.OpenTransitionCompletedCount, Is.EqualTo(openTransitionCountBefore + 1));
		}

		#endregion

		[UnityTest]
		public IEnumerator LoadUiAsync_TypeAddressOverload_LoadsAtExplicitAddress()
		{
			var task = _service.LoadUiAsync(typeof(TestUiPresenter), "explicit_addr", openAfter: false);
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult();

			Assert.IsNotNull(presenter);
			var loaded = _service.GetLoadedPresenters();
			Assert.AreEqual(1, loaded.Count);
			Assert.AreEqual("explicit_addr", loaded[0].Address);
			Assert.AreSame(presenter, loaded[0].Presenter);
		}

		[UnityTest]
		public IEnumerator LoadUiAsyncGeneric_ValidConfig_ReturnsTypedPresenter()
		{
			var task = _service.LoadUiAsync<TestUiPresenter>();
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult();

			Assert.IsNotNull(presenter);
			Assert.IsInstanceOf<TestUiPresenter>(presenter);
			Assert.That(presenter.IsOpen, Is.False);
			var loaded = _service.GetLoadedPresenters();
			Assert.AreEqual(1, loaded.Count);
			Assert.AreSame(presenter, loaded[0].Presenter);
		}

		[UnityTest]
		public IEnumerator UnloadUiGeneric_LoadedSingleton_UnloadsCorrectly()
		{
			var loadTask = _service.LoadUiAsync<TestUiPresenter>();
			yield return loadTask.ToCoroutine();
			Assume.That(_service.GetLoadedPresenters().Count, Is.EqualTo(1));
			var unloadCallsBefore = _mockLoader.UnloadCallCount;

			_service.UnloadUi<TestUiPresenter>();

			Assert.AreEqual(0, _service.GetLoadedPresenters().Count);
			Assert.AreEqual(unloadCallsBefore + 1, _mockLoader.UnloadCallCount);
		}

		[UnityTest]
		public IEnumerator UnloadUiGenericInstance_WithExplicitPresenter_UnloadsAtItsAddress()
		{
			var task1 = _service.LoadUiAsync(typeof(TestUiPresenter), "instA");
			yield return task1.ToCoroutine();
			var task2 = _service.LoadUiAsync(typeof(TestUiPresenter), "instB");
			yield return task2.ToCoroutine();
			var presenterB = (TestUiPresenter) task2.GetAwaiter().GetResult();
			Assume.That(_service.GetLoadedPresenters().Count, Is.EqualTo(2));

			_service.UnloadUi<TestUiPresenter>(presenterB);

			var loaded = _service.GetLoadedPresenters();
			Assert.AreEqual(1, loaded.Count);
			Assert.AreEqual("instA", loaded[0].Address);
		}

		[UnityTest]
		public IEnumerator LoadUiAsync_CancellationRequestedMidLoad_AbortsBeforeAddRegistration()
		{
			_mockLoader.SimulatedDelayMs = 1000;
			var cts = new CancellationTokenSource();

			var task = _service.LoadUiAsync(typeof(TestUiPresenter), "cancel_target", openAfter: false, cts.Token);
			yield return null;
			cts.Cancel();

			while (!task.Status.IsCompleted())
			{
				yield return null;
			}

			System.OperationCanceledException caught = null;
			try
			{
				task.GetAwaiter().GetResult();
			}
			catch (System.OperationCanceledException ex)
			{
				caught = ex;
			}

			Assert.IsNotNull(caught);
			Assert.AreEqual(0, _service.GetLoadedPresenters().Count);
		}
	}
}
