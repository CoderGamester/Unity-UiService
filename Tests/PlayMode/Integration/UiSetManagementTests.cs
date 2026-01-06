using System.Collections;
using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
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
		public IEnumerator OpenUiSetAsync_ValidSet_OpensAllPresenters()
		{
			// Act
			var task = _service.OpenUiSetAsync(1);
			yield return task.ToCoroutine();
			var presenters = task.GetAwaiter().GetResult();

			// Assert
			Assert.AreEqual(2, presenters.Length);
			Assert.AreEqual(2, _service.VisiblePresenters.Count);
			Assert.IsTrue(presenters.All(p => p.gameObject.activeSelf));
		}

		[UnityTest]
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
