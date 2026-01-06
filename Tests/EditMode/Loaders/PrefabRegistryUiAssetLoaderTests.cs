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
		public void UnloadAsset_HandlesNullGracefully()
		{
			// Act & Assert - should not throw
			Assert.DoesNotThrow(() => _loader.UnloadAsset(null));
		}
	}
}

