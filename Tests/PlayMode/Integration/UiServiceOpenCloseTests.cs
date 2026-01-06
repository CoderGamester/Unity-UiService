using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace GameLovers.UiService.Tests.PlayMode
{
	[TestFixture]
	public class UiServiceOpenCloseTests
	{
		private MockAssetLoader _mockLoader;
		private MockAnalytics _mockAnalytics;
		private UiService _service;

		[SetUp]
		public void Setup()
		{
			_mockLoader = new MockAssetLoader();
			_mockLoader.RegisterPrefab<TestUiPresenter>("test_presenter");
			_mockLoader.RegisterPrefab<TestDataUiPresenter>("data_presenter");
			
			_mockAnalytics = new MockAnalytics();
			_service = new UiService(_mockLoader, _mockAnalytics);
			
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
		public IEnumerator OpenUiAsync_LoadedUi_OpensSuccessfully()
		{
			// Arrange
			var loadTask = _service.LoadUiAsync(typeof(TestUiPresenter));
			yield return loadTask.ToCoroutine();

			// Act
			var openTask = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return openTask.ToCoroutine();
			var presenter = openTask.GetAwaiter().GetResult();

			// Assert
			Assert.That(presenter.gameObject.activeSelf, Is.True);
			Assert.That(_service.VisiblePresenters.Count, Is.EqualTo(1));
		}

		[UnityTest]
		public IEnumerator OpenUiAsync_NotLoaded_LoadsAndOpens()
		{
			// Act
			var task = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult();

			// Assert
			Assert.IsNotNull(presenter);
			Assert.That(presenter.gameObject.activeSelf, Is.True);
			Assert.AreEqual(1, _mockLoader.InstantiateCallCount);
		}

		[UnityTest]
		public IEnumerator OpenUiAsync_WithData_SetsData()
		{
			// Arrange
			var data = new TestPresenterData { Id = 42, Name = "Test" };

			// Act
			var task = _service.OpenUiAsync(typeof(TestDataUiPresenter), data);
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestDataUiPresenter;

			// Assert
			Assert.IsNotNull(presenter);
			Assert.That(presenter.WasDataSet, Is.True);
			Assert.AreEqual(42, presenter.ReceivedData.Id);
			Assert.AreEqual("Test", presenter.ReceivedData.Name);
		}

		[UnityTest]
		public IEnumerator CloseUi_OpenUi_ClosesSuccessfully()
		{
			// Arrange
			var task = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult();
			yield return presenter.OpenTransitionTask.ToCoroutine();

			// Act
			_service.CloseUi(typeof(TestUiPresenter));
			yield return presenter.CloseTransitionTask.ToCoroutine();

			// Assert
			Assert.That(presenter.gameObject.activeSelf, Is.False);
			Assert.AreEqual(0, _service.VisiblePresenters.Count);
		}

		[UnityTest]
		public IEnumerator CloseUi_WithDestroy_UnloadsUi()
		{
			// Arrange
			var task = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult();
			yield return presenter.OpenTransitionTask.ToCoroutine();

			// Act
			_service.CloseUi(typeof(TestUiPresenter), destroy: true);
			yield return presenter.CloseTransitionTask.ToCoroutine();

			// Assert
			Assert.AreEqual(1, _mockLoader.UnloadCallCount);
		}

		[UnityTest]
		public IEnumerator CloseAllUi_MultipleOpen_ClosesAll()
		{
			// Arrange
			var task1 = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return task1.ToCoroutine();
			var presenter1 = task1.GetAwaiter().GetResult();
			
			var task2 = _service.OpenUiAsync(typeof(TestDataUiPresenter));
			yield return task2.ToCoroutine();
			var presenter2 = task2.GetAwaiter().GetResult();

			// Act
			_service.CloseAllUi();
			
			// Wait for both to finish closing
			yield return presenter1.CloseTransitionTask.ToCoroutine();
			yield return presenter2.CloseTransitionTask.ToCoroutine();

			// Assert
			Assert.AreEqual(0, _service.VisiblePresenters.Count);
		}

		[UnityTest]
		public IEnumerator CloseAllUi_WithLayer_ClosesOnlyLayer()
		{
			// Arrange
			var task1 = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return task1.ToCoroutine();
			var presenter1 = task1.GetAwaiter().GetResult();
			
			var task2 = _service.OpenUiAsync(typeof(TestDataUiPresenter));
			yield return task2.ToCoroutine();

			// Act - Close layer 0 only
			_service.CloseAllUi(0);
			yield return presenter1.CloseTransitionTask.ToCoroutine();

			// Assert - TestUiPresenter (layer 0) closed, TestDataUiPresenter (layer 1) still visible
			Assert.AreEqual(1, _service.VisiblePresenters.Count);
		}

		[UnityTest]
		public IEnumerator VisiblePresenters_TracksOpenClose()
		{
			// Act 1 - Open
			var task = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult();

			// Assert 1
			Assert.AreEqual(1, _service.VisiblePresenters.Count);

			// Act 2 - Close
			_service.CloseUi(typeof(TestUiPresenter));
			yield return presenter.CloseTransitionTask.ToCoroutine();

			// Assert 2
			Assert.AreEqual(0, _service.VisiblePresenters.Count);
		}

		[UnityTest]
		public IEnumerator Analytics_TracksLifecycleEvents()
		{
			// Act
			var task = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult();
			yield return presenter.OpenTransitionTask.ToCoroutine();
			
			_service.CloseUi(typeof(TestUiPresenter));
			yield return presenter.CloseTransitionTask.ToCoroutine();

			// Assert - Check events were tracked via MockAnalytics verification
			_mockAnalytics.VerifyLoadStartCalled(typeof(TestUiPresenter), times: 1);
			_mockAnalytics.VerifyLoadCompleteCalled(typeof(TestUiPresenter), times: 1);
			_mockAnalytics.VerifyOpenStartCalled(typeof(TestUiPresenter), times: 1);
			_mockAnalytics.VerifyOpenCompleteCalled(typeof(TestUiPresenter), times: 1);
			_mockAnalytics.VerifyCloseStartCalled(typeof(TestUiPresenter), times: 1);
			_mockAnalytics.VerifyCloseCompleteCalled(typeof(TestUiPresenter), times: 1);
		}
		
		[UnityTest]
		public IEnumerator OpenUiAsync_Twice_ReturnsSamePresenter()
		{
			// Act
			var task1 = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return task1.ToCoroutine();
			var p1 = task1.GetAwaiter().GetResult();
			
			var task2 = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return task2.ToCoroutine();
			var p2 = task2.GetAwaiter().GetResult();

			// Assert
			Assert.AreEqual(p1, p2);
			Assert.AreEqual(1, _mockLoader.InstantiateCallCount);
		}

		[UnityTest]
		public IEnumerator CloseUi_NotVisible_DoesNothing()
		{
			// Act
			_service.CloseUi(typeof(TestUiPresenter));

			// Assert
			Assert.Pass();
			yield return null;
		}

		[UnityTest]
		public IEnumerator CloseAllUi_Empty_DoesNothing()
		{
			// Act
			_service.CloseAllUi();

			// Assert
			Assert.AreEqual(0, _service.VisiblePresenters.Count);
			yield return null;
		}

		[UnityTest]
		public IEnumerator OpenTransitionCompleted_AlwaysCalled()
		{
			// Act
			var task = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestUiPresenter;
			yield return presenter.OpenTransitionTask.ToCoroutine();

			// Assert - OnOpenTransitionCompleted should be called even without transition features
			Assert.IsTrue(presenter.WasOpenTransitionCompleted);
			Assert.AreEqual(1, presenter.OpenTransitionCompletedCount);
		}

		[UnityTest]
		public IEnumerator CloseTransitionCompleted_AlwaysCalled()
		{
			// Arrange
			var task = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestUiPresenter;
			yield return presenter.OpenTransitionTask.ToCoroutine();

			// Act
			_service.CloseUi(typeof(TestUiPresenter));
			yield return presenter.CloseTransitionTask.ToCoroutine();

			// Assert - OnCloseTransitionCompleted should be called even without transition features
			Assert.IsTrue(presenter.WasCloseTransitionCompleted);
			Assert.AreEqual(1, presenter.CloseTransitionCompletedCount);
		}

		#region Data Setter Tests

		[UnityTest]
		public IEnumerator SetData_DirectAssignment_CallsOnSetData()
		{
			// Arrange - Open without initial data
			var task = _service.OpenUiAsync(typeof(TestDataUiPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestDataUiPresenter;

			// Act - Set data directly via property setter
			presenter.Data = new TestPresenterData { Id = 99, Name = "Direct" };

			// Assert
			Assert.That(presenter.WasDataSet, Is.True);
			Assert.AreEqual(99, presenter.ReceivedData.Id);
			Assert.AreEqual("Direct", presenter.ReceivedData.Name);
		}

		[UnityTest]
		public IEnumerator SetData_DirectAssignment_DataPropertyReturnsValue()
		{
			// Arrange
			var task = _service.OpenUiAsync(typeof(TestDataUiPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestDataUiPresenter;
			var data = new TestPresenterData { Id = 123, Name = "Readable" };

			// Act
			presenter.Data = data;

			// Assert - Data property getter returns the assigned value
			Assert.AreEqual(123, presenter.Data.Id);
			Assert.AreEqual("Readable", presenter.Data.Name);
		}

		[UnityTest]
		public IEnumerator SetData_MultipleUpdates_CallsOnSetDataEachTime()
		{
			// Arrange
			var task = _service.OpenUiAsync(typeof(TestDataUiPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestDataUiPresenter;

			// Act - Set data multiple times
			presenter.Data = new TestPresenterData { Id = 1, Name = "First" };
			presenter.Data = new TestPresenterData { Id = 2, Name = "Second" };
			presenter.Data = new TestPresenterData { Id = 3, Name = "Third" };

			// Assert - OnSetData should be called 3 times
			Assert.AreEqual(3, presenter.OnSetDataCallCount);
			Assert.AreEqual(3, presenter.Data.Id);
			Assert.AreEqual("Third", presenter.Data.Name);
		}

		[UnityTest]
		public IEnumerator SetData_OnAlreadyOpenPresenter_UpdatesDynamically()
		{
			// Arrange - Open with initial data
			var initialData = new TestPresenterData { Id = 1, Name = "Initial" };
			var task = _service.OpenUiAsync(typeof(TestDataUiPresenter), initialData);
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestDataUiPresenter;
			yield return presenter.OpenTransitionTask.ToCoroutine();

			// Act - Update data while presenter is already open
			var updatedData = new TestPresenterData { Id = 2, Name = "Updated" };
			presenter.Data = updatedData;

			// Assert - OnSetData should be called twice (once on open, once on update)
			Assert.AreEqual(2, presenter.OnSetDataCallCount);
			Assert.AreEqual(2, presenter.Data.Id);
			Assert.AreEqual("Updated", presenter.Data.Name);
		}

		[UnityTest]
		public IEnumerator SetData_ConsecutiveUpdates_PreservesLatestValue()
		{
			// Arrange
			var task = _service.OpenUiAsync(typeof(TestDataUiPresenter));
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestDataUiPresenter;

			// Act - Rapid consecutive updates
			for (var i = 0; i < 10; i++)
			{
				presenter.Data = new TestPresenterData { Id = i, Name = $"Update{i}" };
			}

			// Assert - Only latest value should be retained, OnSetData called 10 times
			Assert.AreEqual(9, presenter.Data.Id);
			Assert.AreEqual("Update9", presenter.Data.Name);
			Assert.AreEqual(10, presenter.OnSetDataCallCount);
		}

		#endregion
	}
}
