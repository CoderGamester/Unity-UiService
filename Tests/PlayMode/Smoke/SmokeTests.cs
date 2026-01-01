using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace GameLovers.UiService.Tests.PlayMode
{
	/// <summary>
	/// Quick validation tests for critical paths.
	/// Target runtime: ~30 seconds total.
	/// </summary>
	[TestFixture]
	public class SmokeTests
	{
		private MockAssetLoader _mockLoader;
		private UiService _service;
	
		[SetUp]
		public void Setup()
		{
			_mockLoader = new MockAssetLoader();
			_mockLoader.RegisterPrefab<TestUiPresenter>("test_presenter");
			_mockLoader.RegisterPrefab<TestDataUiPresenter>("data_presenter");
		}

		[TearDown]
		public void TearDown()
		{
			_service?.Dispose();
			_mockLoader?.Cleanup();
		}

		[UnityTest, Timeout(2000)]
		public IEnumerator Smoke_ServiceInitializes()
		{
			// Act
			_service = new UiService(_mockLoader);
			var configs = TestHelpers.CreateTestConfigs(
				TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "test_presenter", 0)
			);
			_service.Init(configs);

			yield return null;

			// Assert
			Assert.IsNotNull(_service);
		}

		[UnityTest, Timeout(2000)]
		public IEnumerator Smoke_LoadUi()
		{
			// Arrange
			_service = new UiService(_mockLoader);
			_service.Init(TestHelpers.CreateTestConfigs(
				TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "test_presenter", 0)
			));

			// Act
			var task = _service.LoadUiAsync(typeof(TestUiPresenter));
			yield return task.ToCoroutine();

			// Assert
			Assert.IsNotNull(task.GetAwaiter().GetResult());
		}

		[UnityTest, Timeout(2000)]
		public IEnumerator Smoke_OpenUi()
		{
			// Arrange
			_service = new UiService(_mockLoader);
			_service.Init(TestHelpers.CreateTestConfigs(
				TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "test_presenter", 0)
			));

			// Act
			var task = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return task.ToCoroutine();

			// Assert
			Assert.That(_service.VisiblePresenters.Count, Is.EqualTo(1));
		}

		[UnityTest, Timeout(2000)]
		public IEnumerator Smoke_CloseUi()
		{
			// Arrange
			_service = new UiService(_mockLoader);
			_service.Init(TestHelpers.CreateTestConfigs(
				TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "test_presenter", 0)
			));
			var task = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult();
		
			// Act
			_service.CloseUi(typeof(TestUiPresenter));
			yield return presenter.CloseTransitionTask.ToCoroutine();

			// Assert
			Assert.AreEqual(0, _service.VisiblePresenters.Count);
		}

		[UnityTest, Timeout(2000)]
		public IEnumerator Smoke_UnloadUi()
		{
			// Arrange
			_service = new UiService(_mockLoader);
			_service.Init(TestHelpers.CreateTestConfigs(
				TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "test_presenter", 0)
			));
			var task = _service.LoadUiAsync(typeof(TestUiPresenter));
			yield return task.ToCoroutine();

			// Act
			_service.UnloadUi(typeof(TestUiPresenter));

			// Assert
			Assert.AreEqual(0, _service.GetLoadedPresenters().Count);
		}

		[UnityTest, Timeout(3000)]
		public IEnumerator Smoke_OpenWithData()
		{
			// Arrange
			_service = new UiService(_mockLoader);
			_service.Init(TestHelpers.CreateTestConfigs(
				TestHelpers.CreateTestConfig(typeof(TestDataUiPresenter), "data_presenter", 0)
			));
			var data = new TestPresenterData { Id = 1, Name = "Test" };
		
			// Act
			var task = _service.OpenUiAsync(typeof(TestDataUiPresenter), data);
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestDataUiPresenter;

			// Assert
			Assert.IsNotNull(presenter);
			Assert.That(presenter.WasDataSet, Is.True);
		}

		[UnityTest, Timeout(2000)]
		public IEnumerator Smoke_CloseWithDestroy()
		{
			// Arrange
			_service = new UiService(_mockLoader);
			_service.Init(TestHelpers.CreateTestConfigs(
				TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "test_presenter", 0)
			));
			var task = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult();

			// Act
			_service.CloseUi(typeof(TestUiPresenter), destroy: true);
			yield return presenter.CloseTransitionTask.ToCoroutine();

			// Assert
			Assert.AreEqual(0, _service.GetLoadedPresenters().Count);
		}

		[UnityTest, Timeout(2000)]
		public IEnumerator Smoke_CloseAllUi()
		{
			// Arrange
			_service = new UiService(_mockLoader);
			_service.Init(TestHelpers.CreateTestConfigs(
				TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "test_presenter", 0),
				TestHelpers.CreateTestConfig(typeof(TestDataUiPresenter), "data_presenter", 1)
			));
			var task1 = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return task1.ToCoroutine();
			var presenter1 = task1.GetAwaiter().GetResult();
			
			var task2 = _service.OpenUiAsync(typeof(TestDataUiPresenter));
			yield return task2.ToCoroutine();
			var presenter2 = task2.GetAwaiter().GetResult();

			// Act
			_service.CloseAllUi();
			yield return presenter1.CloseTransitionTask.ToCoroutine();
			yield return presenter2.CloseTransitionTask.ToCoroutine();

			// Assert
			Assert.AreEqual(0, _service.VisiblePresenters.Count);
		}

		[UnityTest, Timeout(3000)]
		public IEnumerator Smoke_MultipleInstances()
		{
			// Arrange
			_service = new UiService(_mockLoader);
			_service.Init(TestHelpers.CreateTestConfigs(
				TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "test_presenter", 0)
			));
		
			// Act
			var task1 = _service.OpenUiAsync(typeof(TestUiPresenter), "instance_1");
			yield return task1.ToCoroutine();
			var task2 = _service.OpenUiAsync(typeof(TestUiPresenter), "instance_2");
			yield return task2.ToCoroutine();

			// Assert
			Assert.AreEqual(2, _service.GetLoadedPresenters().Count);
		}

		[UnityTest, Timeout(2000)]
		public IEnumerator Smoke_Analytics()
		{
			// Arrange
			var analytics = new MockAnalytics();
			_service = new UiService(_mockLoader, analytics);
			_service.Init(TestHelpers.CreateTestConfigs(
				TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "test_presenter", 0)
			));
		
			// Act
			var task = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult();
			
			_service.CloseUi(typeof(TestUiPresenter));
			yield return presenter.CloseTransitionTask.ToCoroutine();
		
			// Assert
			// Verification via Substitute is done in integration tests, here we just check it doesn't crash
			Assert.Pass();
		}

		[UnityTest, Timeout(2000)]
		public IEnumerator Smoke_Dispose()
		{
			// Arrange
			_service = new UiService(_mockLoader);
			_service.Init(TestHelpers.CreateTestConfigs(
				TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "test_presenter", 0)
			));
			var task = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return task.ToCoroutine();

			// Act & Assert - Should not throw
			Assert.DoesNotThrow(() => _service.Dispose());
		}
		
		[UnityTest, Timeout(2000)]
		public IEnumerator Smoke_UiSets()
		{
			// Arrange
			_service = new UiService(_mockLoader);
			var set = TestHelpers.CreateTestUiSet(1, new UiInstanceId(typeof(TestUiPresenter)));
			_service.Init(TestHelpers.CreateTestConfigsWithSets(
				new[] { TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "test_presenter", 0) },
				new[] { set }
			));

			// Act
			var tasks = _service.LoadUiSetAsync(1);
			foreach (var task in tasks)
			{
				yield return task.ToCoroutine();
			}

			// Assert
			Assert.AreEqual(1, _service.GetLoadedPresenters().Count);
		}
	}
}
