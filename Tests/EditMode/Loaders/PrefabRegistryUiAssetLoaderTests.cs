using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GameLovers.UiService.Tests
{
	[TestFixture]
	public class PrefabRegistryUiAssetLoaderTests
	{
		private PrefabRegistryUiAssetLoader _loader;
		private GameObject _testPrefab;
		private Transform _parentTransform;
		private List<GameObject> _createdObjects;

		[SetUp]
		public void SetUp()
		{
			_loader = new PrefabRegistryUiAssetLoader();
			_testPrefab = TestHelpers.CreateTestPresenterPrefab<TestUiPresenter>("TestPrefab");
			
			var parentGo = new GameObject("TestParent");
			_parentTransform = parentGo.transform;
			
			_createdObjects = new List<GameObject> { _testPrefab, parentGo };
		}

		[TearDown]
		public void TearDown()
		{
			foreach (var obj in _createdObjects)
			{
				if (obj != null)
				{
					Object.DestroyImmediate(obj);
				}
			}
			_createdObjects.Clear();
		}

		[Test]
		// ADMIT: PrefabRegistryUiAssetLoader.InstantiatePrefab must return a fresh clone of the registered prefab,
		// not the prefab itself — UiService parents and destroys what it gets back.
		// RCR: PrefabRegistryUiAssetLoader.cs InstantiatePrefab — return the prefab directly instead of
		// instantiating → RED (Assert.AreNotSame fails: instance is the registered prefab). 2026-08-02
		public void RegisterPrefab_AddsPrefabToRegistry()
		{
			// Arrange
			const string address = "test/prefab";

			// Act
			_loader.RegisterPrefab(address, _testPrefab);

			// Assert - verify by successfully instantiating
			var config = TestHelpers.CreateTestConfig(typeof(TestUiPresenter), address);
			var task = _loader.InstantiatePrefab(config, _parentTransform);
			var instance = task.GetAwaiter().GetResult();
			
			_createdObjects.Add(instance);
			
			Assert.IsNotNull(instance);
			Assert.AreNotSame(_testPrefab, instance); // Should be a clone, not the original
		}

		[Test]
		// ADMIT: PrefabRegistryUiAssetLoader.RegisterPrefab uses indexer assignment so re-registering an address
		// replaces the entry; an add-only registry would silently keep the stale prefab.
		// RCR: PrefabRegistryUiAssetLoader.cs RegisterPrefab — guard the write with
		// `if (!_prefabMap.ContainsKey(address))` → RED (instance name is TestPrefab(Clone), not NewPrefab).
		// Every single-registration sibling stays green. 2026-08-02
		public void RegisterPrefab_OverwritesExistingEntry()
		{
			// Arrange
			const string address = "test/prefab";
			var newPrefab = TestHelpers.CreateTestPresenterPrefab<TestUiPresenter>("NewPrefab");
			_createdObjects.Add(newPrefab);

			// Act
			_loader.RegisterPrefab(address, _testPrefab);
			_loader.RegisterPrefab(address, newPrefab); // Overwrite

			// Assert
			var config = TestHelpers.CreateTestConfig(typeof(TestUiPresenter), address);
			var task = _loader.InstantiatePrefab(config, _parentTransform);
			var instance = task.GetAwaiter().GetResult();
			
			_createdObjects.Add(instance);
			
			Assert.IsTrue(instance.name.Contains("NewPrefab"));
		}

		[Test]
		// ADMIT: PrefabRegistryUiAssetLoader.InstantiatePrefab throws for an unregistered address rather than
		// returning null, which would surface later as a NullReferenceException inside UiService.LoadUiAsync.
		// RCR: PrefabRegistryUiAssetLoader.cs InstantiatePrefab — replace the throw with
		// `return UniTask.FromResult<GameObject>(null);` → RED (expected KeyNotFoundException, none thrown). 2026-08-02
		public void InstantiatePrefab_ThrowsKeyNotFoundException_WhenPrefabNotRegistered()
		{
			// Arrange
			var config = TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "unregistered/address");

			// Act & Assert
			Assert.Throws<KeyNotFoundException>(() =>
			{
				_loader.InstantiatePrefab(config, _parentTransform).GetAwaiter().GetResult();
			});
		}

		[Test]
		// ADMIT: PrefabRegistryUiAssetLoader.InstantiatePrefab must hand back a deactivated instance — UiService
		// owns the first SetActive(true) via the open lifecycle, so an active one flashes on screen unopened.
		// RCR: PrefabRegistryUiAssetLoader.cs InstantiatePrefab — flip to `instance.SetActive(true);` → RED
		// (Assert.IsFalse(instance.activeSelf) fails). 2026-08-02
		public void InstantiatePrefab_ReturnsInactiveInstance()
		{
			// Arrange
			const string address = "test/prefab";
			_loader.RegisterPrefab(address, _testPrefab);
			var config = TestHelpers.CreateTestConfig(typeof(TestUiPresenter), address);

			// Act
			var task = _loader.InstantiatePrefab(config, _parentTransform);
			var instance = task.GetAwaiter().GetResult();
			
			_createdObjects.Add(instance);

			// Assert
			Assert.IsFalse(instance.activeSelf);
		}

		[Test]
		// ADMIT: PrefabRegistryUiAssetLoader.InstantiatePrefab must forward the parent Transform, or presenters
		// land at scene root instead of under UiService's DontDestroyOnLoad "Ui" object.
		// RCR: PrefabRegistryUiAssetLoader.cs InstantiatePrefab — drop the parent argument from
		// `Object.Instantiate(prefab, parent)` → RED (expected TestParent, was null). 2026-08-02
		public void InstantiatePrefab_SetsCorrectParent()
		{
			// Arrange
			const string address = "test/prefab";
			_loader.RegisterPrefab(address, _testPrefab);
			var config = TestHelpers.CreateTestConfig(typeof(TestUiPresenter), address);

			// Act
			var task = _loader.InstantiatePrefab(config, _parentTransform);
			var instance = task.GetAwaiter().GetResult();
			
			_createdObjects.Add(instance);

			// Assert
			Assert.AreEqual(_parentTransform, instance.transform.parent);
		}

		[Test]
		// ADMIT: PrefabRegistryUiAssetLoader.UnloadAsset must actually destroy the instance; UiService.UnloadUi
		// delegates all teardown to it, so a no-op leaks every unloaded presenter.
		// RCR: PrefabRegistryUiAssetLoader.cs UnloadAsset — replace `Object.DestroyImmediate(asset);` with
		// `_ = asset;` → RED (Assert.IsTrue(instance == null) fails). 2026-08-02
		public void UnloadAsset_DestroysGameObject()
		{
			// Arrange
			const string address = "test/prefab";
			_loader.RegisterPrefab(address, _testPrefab);
			var config = TestHelpers.CreateTestConfig(typeof(TestUiPresenter), address);
			var task = _loader.InstantiatePrefab(config, _parentTransform);
			var instance = task.GetAwaiter().GetResult();

			// Act
			_loader.UnloadAsset(instance);

			// Assert
			Assert.IsTrue(instance == null); // Unity destroys the object
		}

		[Test]
		// ADMIT: PrefabRegistryUiAssetLoader.UnloadAsset's null guard lets UiService.Dispose sweep instances that
		// Unity already destroyed.
		// RCR: none reachable — deleting `if (asset != null)` leaves the test green because
		// Object.DestroyImmediate(null) is itself a no-op, so the guard has nothing observable to protect here.
		// D2 overclaim: the name promises the guard matters; only the double-destroy path would show it. 2026-08-02
		public void UnloadAsset_HandlesNullGracefully()
		{
			// Act & Assert - should not throw
			Assert.DoesNotThrow(() => _loader.UnloadAsset(null));
		}

		[Test]
		// ADMIT: PrefabRegistryUiAssetLoader(PrefabRegistryUiConfigs) must walk configs.PrefabEntries and register
		// each one, or a sample wired purely through the asset resolves no addresses at all.
		// RCR: PrefabRegistryUiAssetLoader.cs constructor — make it return unconditionally (`return;`) → RED
		// (KeyNotFoundException "Prefab not registered for address: ctor/prefab"). 2026-08-02
		public void Constructor_WithConfigsAsset_PrePopulatesRegistryFromEntries()
		{
			var configsAsset = ScriptableObject.CreateInstance<PrefabRegistryUiConfigs>();
			configsAsset.SetPrefabEntries(new List<PrefabRegistryUiConfigs.PrefabEntry>
			{
				new PrefabRegistryUiConfigs.PrefabEntry { Address = "ctor/prefab", Prefab = _testPrefab }
			});

			var loader = new PrefabRegistryUiAssetLoader(configsAsset);

			var config = TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "ctor/prefab");
			var instance = loader.InstantiatePrefab(config, _parentTransform).GetAwaiter().GetResult();
			_createdObjects.Add(instance);

			Assert.IsNotNull(instance);
			Assert.AreNotSame(_testPrefab, instance);

			ScriptableObject.DestroyImmediate(configsAsset);
		}
	}
}

