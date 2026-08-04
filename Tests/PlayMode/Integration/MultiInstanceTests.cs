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
		// ADMIT: UiService.LoadUiAsync's already-loaded check must match on the requested instanceAddress, not on a
		// resolved one — resolving would make the second explicit address alias the first instance.
		// RCR: UiService.cs LoadUiAsync — replace `TryFindPresenter(type, instanceAddress, ...)` with
		// `TryFindPresenter(type, ResolveInstanceAddress(type), ...)` → RED (InstantiateCallCount 1, expected 2). 2026-08-02
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
		// ADMIT: UiService.GetUi<T>(string) must look up the caller's explicit instanceAddress; falling back to
		// ResolveInstanceAddress returns the first instance for every address once more than one exists.
		// RCR: UiService.cs GetUi<T>(string) — replace `TryFindPresenter(typeof(T), instanceAddress, ...)` with
		// `TryFindPresenter(typeof(T), ResolveInstanceAddress(typeof(T)), ...)` → RED (got instance_1). 2026-08-02
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
		// ADMIT: UiService.IsVisible<T>(string) must build its UiInstanceId from the supplied address, or every
		// per-instance visibility query collapses onto the default instance.
		// RCR: UiService.cs IsVisible<T>(string) — replace `new UiInstanceId(typeof(T), instanceAddress)` with
		// `new UiInstanceId(typeof(T))` → RED (IsVisible<TestUiPresenter>("instance_1") was False). 2026-08-02
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
		// ADMIT: UiService.CloseUi must remove only the targeted id from _visibleUiList; a blanket clear closes
		// sibling instances the caller never asked about.
		// RCR: UiService.cs CloseUi(Type, string, bool) — replace `_visibleUiList.Remove(instanceId);` with
		// `_visibleUiList.Clear();` → RED (IsVisible("instance_2") was False). 2026-08-02
		public IEnumerator CloseUi_WithInstanceAddress_ClosesCorrectInstance()
		{
			// Arrange
			var task1 = _service.OpenUiAsync(typeof(TestUiPresenter), "instance_1");
			yield return task1.ToCoroutine();
			var presenter1 = task1.GetAwaiter().GetResult();
			
			var task2 = _service.OpenUiAsync(typeof(TestUiPresenter), "instance_2");
			yield return task2.ToCoroutine();

			// Act
			_service.CloseUi(typeof(TestUiPresenter), "instance_1");
			yield return presenter1.CloseTransitionTask.ToCoroutine();

			// Assert
			Assert.That(_service.IsVisible<TestUiPresenter>("instance_1"), Is.False);
			Assert.That(_service.IsVisible<TestUiPresenter>("instance_2"), Is.True);
		}
	
		[UnityTest]
		// ADMIT: UiService.RemoveUi's instance loop must match the requested address, or unloading one instance
		// removes a different one and the caller keeps a handle to a destroyed GameObject.
		// RCR: UiService.cs RemoveUi(Type, string) — invert `if (instanceList[i].Address == instanceAddress)` to
		// `!=` → RED (expected the surviving loaded[0].Address to be "instance_2", was "instance_1"). 2026-08-02
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

		[UnityTest]
		// ADMIT: UiService.CloseUi<T>(T, bool) must route through the presenter's own InstanceAddress — this is the
		// documented multi-instance self-close fix; the default address closes nothing.
		// RCR: UiService.cs CloseUi<T>(T, bool) — replace `uiPresenter.InstanceAddress` with `string.Empty` → RED
		// (unexpected "is not open" warning; 2 presenters still loaded, expected 1). 2026-08-02
		public IEnumerator CloseWithDestroyTrue_FromPresenter_UnloadsCorrectInstance()
		{
			// Arrange - Load two instances
			var task1 = _service.OpenUiAsync(typeof(TestUiPresenter), "instance_1");
			yield return task1.ToCoroutine();
			var presenter1 = task1.GetAwaiter().GetResult();
			
			var task2 = _service.OpenUiAsync(typeof(TestUiPresenter), "instance_2");
			yield return task2.ToCoroutine();
			var presenter2 = task2.GetAwaiter().GetResult();

			// Act - Close instance_1 with destroy from within the presenter
			// This simulates presenter calling Close(destroy: true)
			_service.CloseUi(presenter1, destroy: true);
			yield return presenter1.CloseTransitionTask.ToCoroutine();
			yield return null; // Wait for unload

			// Assert - Only instance_1 should be unloaded
			var loaded = _service.GetLoadedPresenters();
			Assert.AreEqual(1, loaded.Count, "Only one instance should remain");
			Assert.AreEqual("instance_2", loaded[0].Address, "instance_2 should still be loaded");
		}

		[UnityTest]
		// ADMIT: UiPresenter.Init must store the instanceAddress the service passes it; the presenter's own
		// Close(destroy) path reads it back to unload the right instance.
		// RCR: UiPresenter.cs Init — replace `InstanceAddress = instanceAddress ?? string.Empty;` with
		// `InstanceAddress = string.Empty;` → RED (expected "my_address", was ""). 2026-08-02
		public IEnumerator PresenterInstanceAddress_IsSetCorrectly()
		{
			// Arrange & Act
			var task = _service.LoadUiAsync(typeof(TestUiPresenter), "my_address");
			yield return task.ToCoroutine();
			var presenter = (TestUiPresenter) task.GetAwaiter().GetResult();

			// Assert
			Assert.AreEqual("my_address", presenter.InstanceAddress);
		}

		[UnityTest]
		// ADMIT: UiService.AddUi must pass the registered instanceAddress into UiPresenter.Init, or the presenter's
		// view of its own identity diverges from the service's UiInstance record.
		// RCR: UiService.cs AddUi(T, int, string, bool) — replace `ui.Init(this, instanceAddress);` with
		// `ui.Init(this, string.Empty);` → RED (expected "test_presenter", was ""). 2026-08-02
		public IEnumerator PresenterInstanceAddress_DefaultIsConfigAddress()
		{
			// Arrange & Act
			var task = _service.LoadUiAsync(typeof(TestUiPresenter));
			yield return task.ToCoroutine();
			var presenter = (TestUiPresenter) task.GetAwaiter().GetResult();

			// Assert - Default instance address should be the config's address
			Assert.AreEqual("test_presenter", presenter.InstanceAddress);
		}

		[UnityTest]
		// ADMIT: UiService.RemoveUi<T>() must forward to the Type overload and return its result, or the generic
		// convenience API reports failure while callers assume the presenter was detached.
		// RCR: UiService.cs RemoveUi<T>() — replace `return RemoveUi(typeof(T));` with `return false;` → RED
		// (Assert.IsTrue(removed) fails; loaded count still 1). Only this test calls the no-arg generic. 2026-08-02
		public IEnumerator RemoveUiGeneric_LoadedSingleton_RemovesAndReturnsTrue()
		{
			var loadTask = _service.LoadUiAsync<TestUiPresenter>();
			yield return loadTask.ToCoroutine();
			var presenter = loadTask.GetAwaiter().GetResult();
			Assume.That(presenter, Is.Not.Null);
			Assume.That(_service.GetLoadedPresenters().Count, Is.EqualTo(1));

			var removed = _service.RemoveUi<TestUiPresenter>();

			Assert.IsTrue(removed);
			Assert.AreEqual(0, _service.GetLoadedPresenters().Count);
		}

		[UnityTest]
		// ADMIT: UiService.RemoveUi<T>(T) must remove at the presenter's own InstanceAddress, or passing an explicit
		// multi-instance presenter targets the default instance and removes nothing.
		// RCR: UiService.cs RemoveUi<T>(T) — replace `uiPresenter.InstanceAddress` with `string.Empty` → RED
		// (Assert.IsTrue(removed) fails — the loop finds no instance at the default address). 2026-08-02
		public IEnumerator RemoveUiGenericInstance_WithExplicitPresenter_RemovesAtItsAddress()
		{
			var task1 = _service.LoadUiAsync(typeof(TestUiPresenter), "instA");
			yield return task1.ToCoroutine();
			var task2 = _service.LoadUiAsync(typeof(TestUiPresenter), "instB");
			yield return task2.ToCoroutine();
			var presenterB = (TestUiPresenter) task2.GetAwaiter().GetResult();

			var removed = _service.RemoveUi<TestUiPresenter>(presenterB);

			Assert.IsTrue(removed);
			var loaded = _service.GetLoadedPresenters();
			Assert.AreEqual(1, loaded.Count);
			Assert.AreEqual("instA", loaded[0].Address);
		}

		[UnityTest]
		// ADMIT: UiService.RemoveUi must report false when the type is registered but the requested address is not,
		// so a repeated remove is distinguishable from a successful one.
		// RCR: UiService.cs RemoveUi(Type, string) — change the loop's trailing `return false;` to `return true;` →
		// RED (Assert.IsFalse(secondRemoval) fails). The TryGetValue guard's return is a separate mutation. 2026-08-02
		public IEnumerator RemoveUiTypeAddress_WithExplicitAddress_RemovesOnlyThatInstance()
		{
			var task1 = _service.LoadUiAsync(typeof(TestUiPresenter), "instA");
			yield return task1.ToCoroutine();
			var task2 = _service.LoadUiAsync(typeof(TestUiPresenter), "instB");
			yield return task2.ToCoroutine();

			var firstRemoval = _service.RemoveUi(typeof(TestUiPresenter), "instB");

			Assert.IsTrue(firstRemoval);
			var loaded = _service.GetLoadedPresenters();
			Assert.AreEqual(1, loaded.Count);
			Assert.AreEqual("instA", loaded[0].Address);

			var secondRemoval = _service.RemoveUi(typeof(TestUiPresenter), "instB");

			Assert.IsFalse(secondRemoval);
		}
	}
}
