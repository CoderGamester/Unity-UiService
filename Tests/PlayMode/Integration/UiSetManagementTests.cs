using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameLovers.UiService.Tests.PlayMode
{
	[TestFixture]
	public class UiSetManagementTests
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
			
			// IMPORTANT: UI set uses config addresses to ensure proper address matching
			// between Load/Open/Close/Unload operations
			var set = TestHelpers.CreateTestUiSet(1,
				new UiInstanceId(typeof(TestUiPresenter), "test_presenter"),
				new UiInstanceId(typeof(TestDataUiPresenter), "data_presenter")
			);
			
			var configs = TestHelpers.CreateTestConfigsWithSets(
				new[]
				{
					TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "test_presenter", 0),
					TestHelpers.CreateTestConfig(typeof(TestDataUiPresenter), "data_presenter", 1)
				},
				new[] { set }
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
		// ADMIT: UiService.LoadUiSetAsync must queue a LoadUiAsync per set entry, or it returns an empty task list
		// and the set silently never loads.
		// RCR: UiService.cs LoadUiSetAsync — comment out the `uiTasks.Add(LoadUiAsync(...))` call → RED
		// (expected 2 loaded presenters, was 0). 2026-08-02
		public IEnumerator LoadUiSetAsync_ValidSet_LoadsAllPresenters()
		{
			// Act
			var tasks = _service.LoadUiSetAsync(1);
			foreach (var task in tasks)
			{
				yield return task.ToCoroutine();
			}

			// Assert
			Assert.AreEqual(2, _service.GetLoadedPresenters().Count);
		}
	
		[UnityTest]
		// ADMIT: exercises UiService.LoadUiSetAsync's already-loaded `continue` skip over an entry the caller pre-loaded; no unique one-line pin.
		// RCR: no isolated mutation - reddens under LoadUiSetAsync_ValidSet_LoadsAllPresenters's mutation (radius 4, verified).
		// Shared-path coverage, not a duplicate.
		public IEnumerator LoadUiSetAsync_PartiallyLoaded_LoadsOnlyMissing()
		{
			// Arrange - Pre-load one presenter
			var task = _service.LoadUiAsync(typeof(TestUiPresenter));
			yield return task.ToCoroutine();

			// Act
			var tasks = _service.LoadUiSetAsync(1);
			foreach (var t in tasks)
			{
				yield return t.ToCoroutine();
			}

			// Assert - Only one additional load (total 2 instantiates)
			Assert.AreEqual(2, _mockLoader.InstantiateCallCount);
		}
	
		[UnityTest]
		// ADMIT: UiService.CloseAllUiSet must call CloseUi for every entry in the set, or closing a set is a no-op
		// that leaves the whole screen up.
		// RCR: UiService.cs CloseAllUiSet — comment out `CloseUi(instanceId.PresenterType, instanceId.InstanceAddress);`
		// → RED (expected 0 VisiblePresenters, was 2). 2026-08-02
		public IEnumerator CloseAllUiSet_OpenSet_ClosesAllInSet()
		{
			// Arrange
			var loadTasks = _service.LoadUiSetAsync(1);
			foreach (var task in loadTasks)
			{
				yield return task.ToCoroutine();
			}
			
			var openTask1 = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return openTask1.ToCoroutine();
			var presenter1 = openTask1.GetAwaiter().GetResult();
			
			var openTask2 = _service.OpenUiAsync(typeof(TestDataUiPresenter));
			yield return openTask2.ToCoroutine();
			var presenter2 = openTask2.GetAwaiter().GetResult();

			// Act
			_service.CloseAllUiSet(1);
			
			// Wait for close transitions to complete
			yield return presenter1.CloseTransitionTask.ToCoroutine();
			yield return presenter2.CloseTransitionTask.ToCoroutine();

			// Assert
			Assert.AreEqual(0, _service.VisiblePresenters.Count);
		}
	
		[UnityTest]
		// ADMIT: UiService.UnloadUiSet must call UnloadUi for each found entry, or unloading a set releases nothing
		// and the presenters stay resident.
		// RCR: UiService.cs UnloadUiSet — comment out the guarded `UnloadUi(instanceId.PresenterType, ...)` call →
		// RED (expected 0 loaded presenters, was 2). 2026-08-02
		public IEnumerator UnloadUiSet_LoadedSet_UnloadsAll()
		{
			// Arrange
			var loadTasks = _service.LoadUiSetAsync(1);
			foreach (var task in loadTasks)
			{
				yield return task.ToCoroutine();
			}

			// Act
			_service.UnloadUiSet(1);

			// Assert
			Assert.AreEqual(0, _service.GetLoadedPresenters().Count);
		}
	
		[UnityTest]
		// ADMIT: UiService.RemoveUiSet must return the presenters it detached — callers own their lifetime after a
		// remove-without-unload, and an empty return list leaks them.
		// RCR: UiService.cs RemoveUiSet — comment out `list.Add(ui);` → RED (expected 2 returned presenters, was 0;
		// the loaded count assertion still passes, so this pins the return value specifically). 2026-08-02
		public IEnumerator RemoveUiSet_LoadedSet_RemovesAndReturnsPresenters()
		{
			// Arrange
			var loadTasks = _service.LoadUiSetAsync(1);
			foreach (var task in loadTasks)
			{
				yield return task.ToCoroutine();
			}

			// Act
			var removed = _service.RemoveUiSet(1);

			// Assert
			Assert.AreEqual(2, removed.Count);
			Assert.AreEqual(0, _service.GetLoadedPresenters().Count);
		}

		#region OpenUiSetAsync Tests

		[UnityTest]
		// ADMIT: UiService.OpenUiSetAsync must queue an OpenUiAsync per set entry, or it awaits an empty task list
		// and reports success while opening nothing.
		// RCR: UiService.cs OpenUiSetAsync — comment out the `openTasks.Add(OpenUiAsync(...))` call → RED
		// (expected 2 returned presenters, was 0). 2026-08-02
		public IEnumerator OpenUiSetAsync_ValidSet_OpensAllPresenters()
		{
			// Act
			var task = _service.OpenUiSetAsync(1);
			yield return task.ToCoroutine();
			var presenters = task.GetAwaiter().GetResult();

			// Assert
			Assert.AreEqual(2, presenters.Length);
			Assert.AreEqual(2, _service.VisiblePresenters.Count);
			Assert.IsTrue(presenters.All(p => p.IsOpen));
		}

		[UnityTest]
		// ADMIT: exercises UiService.OpenUiSetAsync's load-on-miss path for a set no entry of which was pre-loaded; no unique one-line pin.
		// RCR: no isolated mutation - reddens under OpenUiSetAsync_ValidSet_OpensAllPresenters's mutation (radius 6, verified).
		// Shared-path coverage, not a duplicate.
		public IEnumerator OpenUiSetAsync_NotLoaded_LoadsAndOpensAll()
		{
			// Act - Open without pre-loading
			var task = _service.OpenUiSetAsync(1);
			yield return task.ToCoroutine();
			var presenters = task.GetAwaiter().GetResult();

			// Assert - Should have loaded and opened both
			Assert.AreEqual(2, presenters.Length);
			Assert.AreEqual(2, _mockLoader.InstantiateCallCount);
			Assert.AreEqual(2, _service.VisiblePresenters.Count);
		}

		[UnityTest]
		// ADMIT: exercises UiService.OpenUiSetAsync's per-entry PresenterType resolution in the returned array; no unique one-line pin.
		// RCR: no isolated mutation - reddens under OpenUiSetAsync_ValidSet_OpensAllPresenters's mutation (radius 6, verified).
		// Shared-path coverage, not a duplicate.
		public IEnumerator OpenUiSetAsync_ReturnsCorrectPresenterTypes()
		{
			// Act
			var task = _service.OpenUiSetAsync(1);
			yield return task.ToCoroutine();
			var presenters = task.GetAwaiter().GetResult();

			// Assert
			Assert.IsTrue(presenters.Any(p => p is TestUiPresenter));
			Assert.IsTrue(presenters.Any(p => p is TestDataUiPresenter));
		}

		[UnityTest]
		// ADMIT: UiService.OpenUiSetAsync must open each entry at the set's InstanceAddress, or set-opened presenters
		// register under an address CloseAllUiSet then cannot find.
		// RCR: UiService.cs OpenUiSetAsync — replace `OpenUiAsync(..., instanceId.InstanceAddress, ct)` with
		// `OpenUiAsync(..., string.Empty, ct)` → RED (expected 0 VisiblePresenters, was 2, plus "is not open"). 2026-08-02
		public IEnumerator OpenUiSetAsync_ThenCloseAllUiSet_ClosesAll()
		{
			// Arrange - Open via set method
			var openTask = _service.OpenUiSetAsync(1);
			yield return openTask.ToCoroutine();
			var presenters = openTask.GetAwaiter().GetResult();

			// Act - Close via set method
			_service.CloseAllUiSet(1);
			
			// Wait for close transitions
			foreach (var presenter in presenters)
			{
				yield return presenter.CloseTransitionTask.ToCoroutine();
			}

			// Assert
			Assert.AreEqual(0, _service.VisiblePresenters.Count);
		}

		#endregion

		#region Cross-Operation Compatibility Tests (Set + Individual methods)

		[UnityTest]
		// ADMIT: exercises UiService.CloseAllUiSet against presenters opened through the individual OpenUiAsync(Type) path; no unique one-line pin.
		// RCR: no isolated mutation - reddens under CloseAllUiSet_OpenSet_ClosesAllInSet's mutation (radius 4, verified).
		// Shared-path coverage, not a duplicate.
		public IEnumerator OpenUiAsync_WithoutPreload_ThenCloseAllUiSet_ClosesCorrectly()
		{
			// This test verifies the fix for the core bug:
			// When opening via OpenUiAsync(Type) without pre-loading via LoadUiSetAsync,
			// CloseAllUiSet should still close the presenters correctly because
			// ResolveInstanceAddress now uses config.Address instead of string.Empty
			
			// Act - Open directly without loading via set first
			var openTask1 = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return openTask1.ToCoroutine();
			var presenter1 = openTask1.GetAwaiter().GetResult();
			
			var openTask2 = _service.OpenUiAsync(typeof(TestDataUiPresenter));
			yield return openTask2.ToCoroutine();
			var presenter2 = openTask2.GetAwaiter().GetResult();

			Assert.AreEqual(2, _service.VisiblePresenters.Count);

			// Act - Close via set method
			_service.CloseAllUiSet(1);
			
			yield return presenter1.CloseTransitionTask.ToCoroutine();
			yield return presenter2.CloseTransitionTask.ToCoroutine();

			// Assert - Should close both, even though they were opened via individual methods
			Assert.AreEqual(0, _service.VisiblePresenters.Count);
		}

		[UnityTest]
		// ADMIT: UiService.ResolveInstanceAddress must return the single loaded instance's own address, or every
		// address-less CloseUi/UnloadUi/GetUi call targets a default instance that does not exist.
		// RCR: UiService.cs ResolveInstanceAddress — replace `return instances[0].Address;` with
		// `return string.Empty;` → RED ("is not open" warning; 2 VisiblePresenters). Broad sibling overlap. 2026-08-02
		public IEnumerator OpenUiSetAsync_ThenCloseUi_ClosesIndividualPresenter()
		{
			// Verify that presenters opened via set can be closed individually
			
			// Arrange - Open via set
			var openTask = _service.OpenUiSetAsync(1);
			yield return openTask.ToCoroutine();
			var presenters = openTask.GetAwaiter().GetResult();
			var testPresenter = presenters.First(p => p is TestUiPresenter);

			// Act - Close one presenter individually
			_service.CloseUi(typeof(TestUiPresenter));
			yield return testPresenter.CloseTransitionTask.ToCoroutine();

			// Assert - Only one should remain visible
			Assert.AreEqual(1, _service.VisiblePresenters.Count);
			Assert.IsFalse(_service.IsVisible<TestUiPresenter>());
			Assert.IsTrue(_service.IsVisible<TestDataUiPresenter>());
		}

		[UnityTest]
		// ADMIT: exercises UiService.CloseAllUiSet against presenters loaded by set but opened individually; no unique one-line pin.
		// RCR: no isolated mutation - reddens under CloseAllUiSet_OpenSet_ClosesAllInSet's mutation (radius 4, verified).
		// Shared-path coverage, not a duplicate.
		public IEnumerator LoadUiSetAsync_ThenOpenUiAsync_ThenCloseAllUiSet_ClosesAll()
		{
			// Verify the scenario: Load via set, open via individual, close via set
			
			// Arrange - Load via set
			var loadTasks = _service.LoadUiSetAsync(1);
			foreach (var task in loadTasks)
			{
				yield return task.ToCoroutine();
			}

			// Open via individual methods
			var openTask1 = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return openTask1.ToCoroutine();
			var presenter1 = openTask1.GetAwaiter().GetResult();
			
			var openTask2 = _service.OpenUiAsync(typeof(TestDataUiPresenter));
			yield return openTask2.ToCoroutine();
			var presenter2 = openTask2.GetAwaiter().GetResult();

			// Act - Close via set
			_service.CloseAllUiSet(1);
			
			yield return presenter1.CloseTransitionTask.ToCoroutine();
			yield return presenter2.CloseTransitionTask.ToCoroutine();

			// Assert
			Assert.AreEqual(0, _service.VisiblePresenters.Count);
		}

		[UnityTest]
		// ADMIT: UiService.RemoveUi must drop the id from _visibleUiList, or unloading a still-open presenter leaves
		// a dangling entry in VisiblePresenters pointing at a destroyed GameObject.
		// RCR: UiService.cs RemoveUi(Type, string) — comment out `_visibleUiList.Remove(instanceId);` → RED
		// (expected 0 VisiblePresenters, was 2). Distinct line from CloseUi's own Remove. 2026-08-02
		public IEnumerator OpenUiAsync_WithoutPreload_ThenUnloadUiSet_UnloadsAll()
		{
			// Verify that UnloadUiSet works with presenters opened via individual methods
			
			// Arrange - Open directly without loading via set
			var openTask1 = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return openTask1.ToCoroutine();
			
			var openTask2 = _service.OpenUiAsync(typeof(TestDataUiPresenter));
			yield return openTask2.ToCoroutine();

			// Act - Unload via set (should close and unload)
			_service.UnloadUiSet(1);

			// Assert
			Assert.AreEqual(0, _service.GetLoadedPresenters().Count);
			Assert.AreEqual(0, _service.VisiblePresenters.Count);
		}

		#endregion

		#region ResolveInstanceAddress Consistency Tests

		[UnityTest]
		// ADMIT: UiService.OpenUiAsync(Type) must use config.Address, not string.Empty, as the default instance
		// address — that consistency is what lets set operations later find individually-opened presenters.
		// RCR: UiService.cs OpenUiAsync(Type, CancellationToken) — replace `config.Address` with `string.Empty` in
		// the forwarding call → RED (expected loaded[0].Address "test_presenter", was ""). 2026-08-02
		public IEnumerator OpenUiAsync_UsesConfigAddressWhenNotLoaded()
		{
			// This tests that ResolveInstanceAddress returns config.Address
			// when no instance is loaded yet
			
			// Act - Open without loading first
			var openTask = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return openTask.ToCoroutine();

			// Assert - Verify the presenter was loaded with the config address
			var loaded = _service.GetLoadedPresenters();
			Assert.AreEqual(1, loaded.Count);
			Assert.AreEqual("test_presenter", loaded[0].Address);
		}

		[UnityTest]
		// ADMIT: UiService.LoadUiAsync(Type) must use config.Address as the default instance address, matching the
		// OpenUiAsync(Type) convention that UI-set lookups depend on.
		// RCR: UiService.cs LoadUiAsync(Type, bool, CancellationToken) — replace `config.Address` with
		// `string.Empty` in the forwarding call → RED (expected loaded[0].Address "test_presenter", was ""). 2026-08-02
		public IEnumerator LoadUiAsync_WithoutAddress_UsesConfigAddress()
		{
			// Verify that LoadUiAsync(Type) without explicit address uses config.Address
			
			// Act
			var loadTask = _service.LoadUiAsync(typeof(TestUiPresenter));
			yield return loadTask.ToCoroutine();

			// Assert
			var loaded = _service.GetLoadedPresenters();
			Assert.AreEqual(1, loaded.Count);
			Assert.AreEqual("test_presenter", loaded[0].Address);
		}

		#endregion

		#region Unload Set with Open Presenters

		[UnityTest]
		public IEnumerator UnloadUiSet_WithOpenPresenters_UnloadsAll()
		{
			// Verify that UnloadUiSet works even when presenters are still open
			
			// Arrange - Open via set
			var openTask = _service.OpenUiSetAsync(1);
			yield return openTask.ToCoroutine();
			
			Assert.AreEqual(2, _service.VisiblePresenters.Count);

			// Act - Unload while still open
			_service.UnloadUiSet(1);

			// Assert - Should unload (and implicitly close)
			Assert.AreEqual(0, _service.GetLoadedPresenters().Count);
		}

		#endregion

		#region OpenUiSetAsync Edge Cases

		[Test]
		// ADMIT: UiService.OpenUiSetAsync must reject an unknown setId with KeyNotFoundException; callers catch that
		// specific type, and any other exception escapes their handler.
		// RCR: UiService.cs OpenUiSetAsync — change the throw to `InvalidOperationException` → RED (Assert.ThrowsAsync
		// reports InvalidOperationException where KeyNotFoundException was expected). 2026-08-02
		public void OpenUiSetAsync_InvalidSetId_ThrowsKeyNotFoundException()
		{
			// Assert
			Assert.ThrowsAsync<System.Collections.Generic.KeyNotFoundException>(async () => 
				await _service.OpenUiSetAsync(999));
		}

		[UnityTest]
		public IEnumerator OpenUiSetAsync_CalledTwice_DoesNotDuplicatePresenters()
		{
			// First open
			var task1 = _service.OpenUiSetAsync(1);
			yield return task1.ToCoroutine();

			// Expected: each already-open entry in the set logs an "already open" warning.
			// The set contains two presenters, so expect one warning per entry.
			LogAssert.Expect(LogType.Warning, new Regex("is already open"));
			LogAssert.Expect(LogType.Warning, new Regex("is already open"));

			// Second open (should not duplicate)
			var task2 = _service.OpenUiSetAsync(1);
			yield return task2.ToCoroutine();
			
			// Assert - still only 2 presenters
			Assert.AreEqual(2, _service.VisiblePresenters.Count);
			Assert.AreEqual(2, _service.GetLoadedPresenters().Count);
		}

		#endregion
	}
}
