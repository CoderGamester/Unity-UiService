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
		// ADMIT: UiService.LoadUiAsync must return an instantiated presenter that is NOT open - load and
		// open are separate operations and openAfter defaults to false.
		// RCR: UiService.cs AddUi(T, int, string, bool) - `if (openAfter)` -> `if (true)` -> RED
		// (presenter.IsOpen expected False, was True).
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
		// ADMIT: UiService.AddUi must honour openAfter by calling OpenUi, or `LoadUiAsync(openAfter: true)` loads the
		// presenter and leaves it hidden.
		// RCR: UiService.cs AddUi(T, int, string, bool) — replace `if (openAfter)` with `if (false)` → RED
		// (presenter.IsOpen was False; VisiblePresenters 0). 2026-08-02
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
		// ADMIT: UiService.LoadUiAsync's pre-await guard is what makes a repeat load a cache hit; without it the
		// service re-instantiates the prefab and only the post-await double check saves it.
		// RCR: UiService.cs LoadUiAsync — replace `if (TryFindPresenter(type, instanceAddress, out var existingUi))`
		// with `if (false)` → RED (InstantiateCallCount 2, expected 1; "was already loaded" never logged). 2026-08-02
		// The bare `if (false)` is NOT usable here: it leaves `out var existingUi` unassigned and
		// fails to compile, which the harness reports as a missing report rather than a red.
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
		// ADMIT: UiService.LoadUiAsync(Type) must surface a missing UiConfig as KeyNotFoundException; callers key their
		// recovery on that type, and it names AddUiConfig as the fix.
		// RCR: UiService.cs LoadUiAsync(Type, bool, CancellationToken) — change the throw to
		// `InvalidOperationException` → RED (the awaited task faults with the wrong type; caughtException stays null). 2026-08-02
		[Timeout(10000)]
		public IEnumerator LoadUiAsync_MissingConfig_ThrowsException()
		{
			// Arrange - Remove configs and recreate service with no TestUiPresenter config
			_service.Dispose();
			_service = new UiService(_mockLoader);
			_service.Init(TestHelpers.CreateTestConfigs());

			// Act & Assert
			KeyNotFoundException caughtException = null;
			var task = _service.LoadUiAsync(typeof(TestUiPresenter));

			var frameGuard = 0;
			while (!task.Status.IsCompleted())
			{
				if (++frameGuard > 300) // ~5s at 60fps; no simulated delay in this test, so this is a generous cap
				{
					Assert.Fail($"LoadUiAsync_MissingConfig_ThrowsException: task did not complete within {frameGuard} frames");
				}

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
		// ADMIT: UiService.UnloadUi must hand the GameObject back to the asset loader; deregistering alone leaks the
		// Addressables instance for the lifetime of the session.
		// RCR: UiService.cs UnloadUi(Type, string) — comment out `_assetLoader.UnloadAsset(ui.gameObject);` → RED
		// (expected 1 UnloadCallCount, was 0). The RemoveUi half is pinned by UnloadUiSet_WithOpenPresenters. 2026-08-02
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
		// ADMIT: UiService.LoadUiAsync must thread the caller's CancellationToken into IUiAssetLoader.InstantiatePrefab,
		// or a cancelled load runs to completion and registers a presenter nobody asked for.
		// RCR: UiService.cs LoadUiAsync — replace the `cancellationToken` argument to InstantiatePrefab with
		// `CancellationToken.None` → RED (no OperationCanceledException; caughtException stays null). 2026-08-02
		[Timeout(10000)]
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
			var frameGuard = 0;
			while (!task.Status.IsCompleted())
			{
				if (++frameGuard > 300) // ~5s at 60fps; comfortably exceeds the test's own 1000ms simulated delay
				{
					Assert.Fail($"LoadUiAsync_WithCancellation_CancelsOperation: task did not complete within {frameGuard} frames");
				}

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
		// ADMIT: exercises UiService.LoadUiAsync's already-loaded early return leaving an open presenter's visibility untouched; no unique one-line pin.
		// RCR: no isolated mutation - reddens under LoadUiAsync_WithoutAddress_UsesConfigAddress's mutation (radius 7, verified).
		// Shared-path coverage, not a duplicate.
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
		// ADMIT: exercises UiService.LoadUiAsync's already-loaded early return short-circuiting before the openAfter branch; no unique one-line pin.
		// RCR: no isolated mutation - reddens under LoadUiAsync_WithoutAddress_UsesConfigAddress's mutation (radius 7, verified).
		// Shared-path coverage, not a duplicate.
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
		// ADMIT: UiPresenter.InternalOpenProcessAsync must reactivate the GameObject on every open, not just the first
		// — a close-then-reopen cycle otherwise returns the same instance permanently hidden.
		// RCR: UiPresenter.cs InternalOpenProcessAsync — comment out `gameObject.SetActive(true);` → RED
		// (reopenedPresenter.IsOpen was False). First-open siblings redden on the same line. 2026-08-02
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
		// ADMIT: UiPresenter.InternalCloseProcessAsync must call the OnClosed hook, which subclasses use to release
		// per-open subscriptions before the close transition runs.
		// RCR: UiPresenter.cs InternalCloseProcessAsync — comment out `OnClosed();` → RED (CloseCount unchanged).
		// The OnCloseTransitionCompleted half is pinned by CloseTransitionCompleted_AlwaysCalled. 2026-08-02
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
		// ADMIT: exercises UiService.LoadUiAsync's already-loaded early return skipping openAfter on a closed instance; no unique one-line pin.
		// RCR: no isolated mutation - reddens under OpenUiAsync_TypeAddressOverload_OpensInstanceAtAddress's mutation (radius 77, verified).
		// Shared-path coverage, not a duplicate.
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
		// ADMIT: UiPresenter.InternalOpenProcessAsync must call the OnOpened hook after activation, which subclasses
		// use to bind per-open state.
		// RCR: UiPresenter.cs InternalOpenProcessAsync — comment out `OnOpened();` → RED (OpenCount unchanged).
		// The OnOpenTransitionCompleted half is pinned by OpenTransitionCompleted_AlwaysCalled. 2026-08-02
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
		// ADMIT: exercises UiService.LoadUiAsync(Type, string, bool, CancellationToken)'s explicit-instanceAddress registration; no unique one-line pin.
		// RCR: no isolated mutation - reddens under OpenUiAsync_TypeAddressOverload_OpensInstanceAtAddress's mutation (radius 77, verified).
		// Shared-path coverage, not a duplicate.
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
		// ADMIT: UiService.LoadUiAsync<T> must delegate to the Type overload and cast the result; it is the typed
		// entry point every caller uses instead of hand-casting.
		// RCR: UiService.cs LoadUiAsync<T> — replace `return await LoadUiAsync(typeof(T), openAfter, ct) as T;`
		// with `return null;` → RED (Assert.IsNotNull fails). Other LoadUiAsync<T> callers redden too. 2026-08-02
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
		// ADMIT: UiService.UnloadUi<T>() must forward to the Type overload, or the generic convenience API silently
		// unloads nothing.
		// RCR: UiService.cs UnloadUi<T>() — replace `UnloadUi(typeof(T));` with `_ = typeof(T);` → RED (expected 0
		// loaded presenters, was 1; UnloadCallCount unchanged). Only this test calls the no-arg generic. 2026-08-02
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
		// ADMIT: UiService.UnloadUi<T>(T) must unload at the presenter's own InstanceAddress, or passing an explicit
		// multi-instance presenter targets the default instance instead.
		// RCR: UiService.cs UnloadUi<T>(T) — replace `uiPresenter.InstanceAddress` with `string.Empty` → RED
		// (KeyNotFoundException "Cannot unload UI ... It is not loaded" instead of unloading instB). 2026-08-02
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
		// ADMIT: exercises UiService.LoadUiAsync aborting at the awaited InstantiatePrefab so AddUi never registers the instance; no unique one-line pin.
		// RCR: no isolated mutation - reddens under LoadUiAsync_WithCancellation_CancelsOperation's mutation (radius 2, verified).
		// Shared-path coverage, not a duplicate.
		[Timeout(10000)]
		public IEnumerator LoadUiAsync_CancellationRequestedMidLoad_AbortsBeforeAddRegistration()
		{
			_mockLoader.SimulatedDelayMs = 1000;
			var cts = new CancellationTokenSource();

			var task = _service.LoadUiAsync(typeof(TestUiPresenter), "cancel_target", openAfter: false, cts.Token);
			yield return null;
			cts.Cancel();

			var frameGuard = 0;
			while (!task.Status.IsCompleted())
			{
				if (++frameGuard > 300) // ~5s at 60fps; comfortably exceeds the test's own 1000ms simulated delay
				{
					Assert.Fail($"LoadUiAsync_CancellationRequestedMidLoad_AbortsBeforeAddRegistration: task did not complete within {frameGuard} frames");
				}

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

		[UnityTest]
		[Timeout(10000)]
		// ADMIT: UiPresenter.InternalCloseProcessAsync's post-await guard must short-circuit when the presenter's
		// GameObject is destroyed mid-transition, or the resumed method calls SetActive(false) on a dead object.
		// RCR: UiPresenter.cs InternalCloseProcessAsync — delete the
		// `if (!this) { _closeTransitionCompletion?.TrySetResult(); return; }` block → RED (the frame guard below
		// trips: the fire-and-forget faults with MissingReferenceException, which .Forget() swallows, so
		// CloseTransitionTask never completes). 2026-08-01
		public IEnumerator CloseAsync_WhenPresenterDestroyedDuringCloseTransition_CompletesTaskWithoutException()
		{
			// Arrange - a presenter whose close transition spans multiple frames (TimeDelayFeature close delay = 0.05s)
			_mockLoader.RegisterPrefab<TestTimeDelayPresenter>("time_delay_presenter");
			_service.AddUiConfig(TestHelpers.CreateTestConfig(typeof(TestTimeDelayPresenter), "time_delay_presenter", 2));

			var openTask = _service.OpenUiAsync(typeof(TestTimeDelayPresenter));
			yield return openTask.ToCoroutine();
			var presenter = openTask.GetAwaiter().GetResult() as TestTimeDelayPresenter;
			yield return presenter.OpenTransitionTask.ToCoroutine();

			Assert.That(presenter.IsOpen, Is.True);

			// Act - start the close transition. This passes UiPresenter's pre-await guard (~line 141) synchronously
			// and suspends at `await WaitForCloseTransitionsAsync()`, waiting on TimeDelayFeature's close delay.
			_service.CloseUi(typeof(TestTimeDelayPresenter));
			var closeTask = presenter.CloseTransitionTask;

			yield return null; // let the close delay start ticking, landing us between the two guards

			// Destroy the presenter's GameObject mid-transition, before the close delay elapses and the
			// post-await guard (~line 150, "Check again after await") gets evaluated.
			Object.Destroy(presenter.gameObject);

			System.Exception caught = null;

			// Bounded on wall-clock time, not frame count: see the RCR comment on
			// LoadUiAsync_TwoOverlappingCallsForSameType_UnloadsTheDuplicateAndReturnsOneInstance in this same
			// file for why a fixed frame budget is unreliable for a UniTask.Delay(TimeSpan)-based wait in
			// batchmode (DelayType.DeltaTime accumulates Time.deltaTime, and batchmode's tick rate is
			// unbounded/variable). 5s real time comfortably exceeds the 0.05s close delay regardless of tick rate.
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			while (!closeTask.Status.IsCompleted())
			{
				if (stopwatch.ElapsedMilliseconds > 5000)
				{
					Assert.Fail($"CloseAsync_WhenPresenterDestroyedDuringCloseTransition_CompletesTaskWithoutException: task did not complete within {stopwatch.ElapsedMilliseconds}ms");
				}

				yield return null;
			}

			try
			{
				closeTask.GetAwaiter().GetResult();
			}
			catch (System.Exception ex)
			{
				caught = ex;
			}

			// Assert
			Assert.IsNull(caught);
			Assert.That(closeTask.Status.IsCompleted(), Is.True);
		}

		[UnityTest]
		[Timeout(10000)]
		// ADMIT: UiService.LoadUiAsync's post-await double check must catch a second concurrent call for the same
		// type/address whose await resolves after the first already registered its presenter; otherwise both
		// register and the service tracks two instances for one UiInstanceId.
		// RCR: UiService.cs LoadUiAsync — delete the post-await TryFindPresenter unload-and-return block → RED
		// (UnloadCallCount becomes 0 and the two returned presenters are different instances — a silent duplicate
		// registration with no exception). 2026-08-01
		public IEnumerator LoadUiAsync_TwoOverlappingCallsForSameType_UnloadsTheDuplicateAndReturnsOneInstance()
		{
			// Arrange - a nonzero simulated delay keeps InstantiatePrefab pending so a second LoadUiAsync call
			// for the same type can start (and increment InstantiateCallCount) before the first call resolves.
			_mockLoader.SimulatedDelayMs = 50;

			// Act - start two overlapping loads for the same type WITHOUT awaiting either first
			var task1 = _service.LoadUiAsync(typeof(TestUiPresenter));
			var task2 = _service.LoadUiAsync(typeof(TestUiPresenter));

			Assert.AreEqual(2, _mockLoader.InstantiateCallCount, "Both calls must have started InstantiatePrefab before either completed");

			// Bounded on wall-clock time, not frame count: UniTask.Delay(TimeSpan) defaults to
			// DelayType.DeltaTime, and batchmode's Update-tick rate is unbounded/variable (no vsync target),
			// so a fixed frame budget assuming "~60fps" can represent far less than 50ms of accumulated
			// Time.deltaTime under load — a 300-frame cap was observed timing out on this exact test even
			// though the tasks completed correctly, just slower in ticks than expected. 5s real time
			// comfortably exceeds the 50ms simulated delay regardless of tick rate.
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			while (!task1.Status.IsCompleted() || !task2.Status.IsCompleted())
			{
				if (stopwatch.ElapsedMilliseconds > 5000)
				{
					Assert.Fail($"LoadUiAsync_TwoOverlappingCallsForSameType_UnloadsTheDuplicateAndReturnsOneInstance: tasks did not complete within {stopwatch.ElapsedMilliseconds}ms");
				}

				yield return null;
			}

			var presenter1 = task1.GetAwaiter().GetResult();
			var presenter2 = task2.GetAwaiter().GetResult();

			// Assert
			Assert.AreEqual(2, _mockLoader.InstantiateCallCount);
			Assert.AreEqual(1, _mockLoader.UnloadCallCount);
			Assert.AreSame(presenter1, presenter2);
			Assert.AreEqual(1, _service.GetLoadedPresenters().Count);
		}
	}
}
