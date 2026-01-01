using System.Collections;
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
			
			var set = TestHelpers.CreateTestUiSet(1,
				new UiInstanceId(typeof(TestUiPresenter)),
				new UiInstanceId(typeof(TestDataUiPresenter))
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
	}
}
