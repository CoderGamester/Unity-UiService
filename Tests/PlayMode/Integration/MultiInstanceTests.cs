using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace GameLovers.UiService.Tests.PlayMode
{
	[TestFixture]
	public class MultiInstanceTests
	{
		private MockAssetLoader _mockLoader;
		private UiService _service;

		[SetUp]
		public void Setup()
		{
			_mockLoader = new MockAssetLoader();
			_mockLoader.RegisterPrefab<TestUiPresenter>("test_presenter");
			
			_service = new UiService(_mockLoader);
			
			var configs = TestHelpers.CreateTestConfigs(
				TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "test_presenter", 0)
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
		public IEnumerator LoadUi_WithInstanceAddress_CreatesMultipleInstances()
		{
			// Act
			var task1 = _service.LoadUiAsync(typeof(TestUiPresenter), "instance_1");
			yield return task1.ToCoroutine();
			var task2 = _service.LoadUiAsync(typeof(TestUiPresenter), "instance_2");
			yield return task2.ToCoroutine();

			// Assert
			Assert.AreEqual(2, _mockLoader.InstantiateCallCount);
			Assert.AreEqual(2, _service.GetLoadedPresenters().Count);
		}

		[UnityTest]
		public IEnumerator GetUi_WithInstanceAddress_ReturnsCorrectInstance()
		{
			// Arrange
			var task1 = _service.LoadUiAsync(typeof(TestUiPresenter), "instance_1");
			yield return task1.ToCoroutine();
			var presenter1 = task1.GetAwaiter().GetResult();
			
			var task2 = _service.LoadUiAsync(typeof(TestUiPresenter), "instance_2");
			yield return task2.ToCoroutine();
			var presenter2 = task2.GetAwaiter().GetResult();

			// Act
			var retrieved = _service.GetUi<TestUiPresenter>("instance_2");

			// Assert
			Assert.AreEqual(presenter2, retrieved);
		}

		[UnityTest]
		public IEnumerator IsVisible_WithInstanceAddress_ChecksCorrectInstance()
		{
			// Arrange
			var task1 = _service.OpenUiAsync(typeof(TestUiPresenter), "instance_1");
			yield return task1.ToCoroutine();
			
			var task2 = _service.LoadUiAsync(typeof(TestUiPresenter), "instance_2");
			yield return task2.ToCoroutine();

			// Assert
			Assert.That(_service.IsVisible<TestUiPresenter>("instance_1"), Is.True);
			Assert.That(_service.IsVisible<TestUiPresenter>("instance_2"), Is.False);
		}
	
		[UnityTest]
		public IEnumerator CloseUi_WithInstanceAddress_ClosesCorrectInstance()
		{
			// Arrange
			var task1 = _service.OpenUiAsync(typeof(TestUiPresenter), "instance_1");
			yield return task1.ToCoroutine();
			var task2 = _service.OpenUiAsync(typeof(TestUiPresenter), "instance_2");
			yield return task2.ToCoroutine();

			// Act
			_service.CloseUi(typeof(TestUiPresenter), "instance_1");

			// Assert
			Assert.That(_service.IsVisible<TestUiPresenter>("instance_1"), Is.False);
			Assert.That(_service.IsVisible<TestUiPresenter>("instance_2"), Is.True);
		}
	
		[UnityTest]
		public IEnumerator UnloadUi_WithInstanceAddress_UnloadsCorrectInstance()
		{
			// Arrange
			var task1 = _service.LoadUiAsync(typeof(TestUiPresenter), "instance_1");
			yield return task1.ToCoroutine();
			var task2 = _service.LoadUiAsync(typeof(TestUiPresenter), "instance_2");
			yield return task2.ToCoroutine();

			// Act
			_service.UnloadUi(typeof(TestUiPresenter), "instance_1");

			// Assert
			var loaded = _service.GetLoadedPresenters();
			Assert.AreEqual(1, loaded.Count);
			Assert.AreEqual("instance_2", loaded[0].Address);
		}
	}
}
