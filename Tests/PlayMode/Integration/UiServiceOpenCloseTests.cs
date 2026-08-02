using System.Collections;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameLovers.UiService.Tests.PlayMode
{
	[TestFixture]
	public class UiServiceOpenCloseTests
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
			Assert.That(presenter.IsOpen, Is.True);
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
			Assert.That(presenter.IsOpen, Is.True);
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
			Assert.That(presenter.IsOpen, Is.False);
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
		// ADMIT: UiService.CloseAllUi(int layer) indexes _uiConfigs unguarded, but _uiConfigs is populated only by
		// AddUiConfig — a presenter registered via AddUi<T> throws KeyNotFoundException as soon as its layer closes.
		// RCR: UiService.cs CloseAllUi — revert to the unguarded
		// `if (_uiConfigs[instanceId.PresenterType].Layer == layer)` → RED (KeyNotFoundException). 2026-08-01
		public IEnumerator CloseAllUi_WithLayer_WhenPresenterWasAddedWithoutUiConfig_DoesNotThrow()
		{
			// Arrange - register a presenter via the public AddUi API only, bypassing the config-driven load path
			// so _uiConfigs never receives an entry for its type
			var prefab = TestHelpers.CreateTestPresenterPrefab<TestLayerUiPresenter>();
			var presenter = prefab.GetComponent<TestLayerUiPresenter>();
			_service.AddUi(presenter, 5, openAfter: true);

			// Act & Assert
			Assert.DoesNotThrow(() => _service.CloseAllUi(5));

			UnityEngine.Object.DestroyImmediate(prefab);
			yield return null;
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
		public IEnumerator OpenUiAsync_Twice_ReturnsSamePresenter()
		{
			// Act
			var task1 = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return task1.ToCoroutine();
			var p1 = task1.GetAwaiter().GetResult();

			// Expected: second open hits the "already open" warning by design
			LogAssert.Expect(LogType.Warning, new Regex("is already open"));

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
			// Expected: closing a not-open presenter logs a warning by design
			LogAssert.Expect(LogType.Warning, new Regex("but is not open"));

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

		[UnityTest]
		public IEnumerator OpenUiAsync_TypeAddressOverload_OpensInstanceAtAddress()
		{
			var task = _service.OpenUiAsync(typeof(TestUiPresenter), "addr_a");
			yield return task.ToCoroutine();
			var presenterA = task.GetAwaiter().GetResult();
			yield return presenterA.OpenTransitionTask.ToCoroutine();

			Assert.IsNotNull(presenterA);
			Assert.That(presenterA.IsOpen, Is.True);
			Assert.That(_service.VisiblePresenters.Count, Is.EqualTo(1));

			var loaded = _service.GetLoadedPresenters();
			Assert.AreEqual(1, loaded.Count);
			Assert.AreEqual("addr_a", loaded[0].Address);

			Assert.IsFalse(_service.IsVisible<TestUiPresenter>(string.Empty));
			Assert.IsTrue(_service.IsVisible<TestUiPresenter>("addr_a"));
		}

		[UnityTest]
		public IEnumerator OpenUiAsyncGeneric_LoadedPresenter_OpensSuccessfully()
		{
			var task = _service.OpenUiAsync<TestUiPresenter>();
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult();
			yield return presenter.OpenTransitionTask.ToCoroutine();

			Assert.IsNotNull(presenter);
			Assert.IsInstanceOf<TestUiPresenter>(presenter);
			Assert.That(presenter.IsOpen, Is.True);
			Assert.That(_service.IsVisible<TestUiPresenter>(), Is.True);
			Assert.That(_service.VisiblePresenters.Count, Is.EqualTo(1));
		}

		[UnityTest]
		public IEnumerator OpenUiAsyncGenericWithData_LoadedPresenter_OpensAndPassesData()
		{
			var data = new TestPresenterData { Id = 123, Name = "GenericData" };

			var task = _service.OpenUiAsync<TestDataUiPresenter, TestPresenterData>(data);
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult();

			Assert.IsNotNull(presenter);
			Assert.IsInstanceOf<TestDataUiPresenter>(presenter);
			Assert.That(presenter.WasDataSet, Is.True);
			Assert.AreEqual(123, presenter.ReceivedData.Id);
			Assert.AreEqual("GenericData", presenter.ReceivedData.Name);
		}

		[UnityTest]
		public IEnumerator OpenUiAsync_TypeAddressDataOverload_PassesDataToPresenter()
		{
			var data = new TestPresenterData { Id = 7, Name = "Address" };

			var task = _service.OpenUiAsync(typeof(TestDataUiPresenter), "data_addr", data);
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult() as TestDataUiPresenter;

			Assert.IsNotNull(presenter);
			Assert.IsTrue(presenter.WasDataSet);
			Assert.AreEqual(7, presenter.ReceivedData.Id);
			Assert.AreEqual("Address", presenter.ReceivedData.Name);

			var loaded = _service.GetLoadedPresenters();
			Assert.AreEqual(1, loaded.Count);
			Assert.AreEqual("data_addr", loaded[0].Address);
		}

		[UnityTest]
		// ADMIT: UiService.EnsureCanvasSortingOrder must assign canvas.sortingOrder = layer, or every presenter
		// keeps Unity's default 0 regardless of UiConfig.Layer while load and open still succeed.
		// RCR: UiService.cs EnsureCanvasSortingOrder — comment out `canvas.sortingOrder = layer;` → RED
		// (expected 1, was 0). 2026-08-01
		public IEnumerator LoadUiAsync_WithLayerInUiConfig_SetsCanvasSortingOrderToLayer()
		{
			// Arrange - TestDataUiPresenter is configured on layer 1 in Setup()

			// Act
			var task = _service.LoadUiAsync<TestDataUiPresenter>();
			yield return task.ToCoroutine();
			var presenter = task.GetAwaiter().GetResult();

			// Assert
			var canvas = presenter.GetComponent<Canvas>();
			Assert.AreEqual(1, canvas.sortingOrder);
		}
	}
}
