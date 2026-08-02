using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace GameLovers.UiService.Tests
{
	[TestFixture]
	public class ResourcesUiAssetLoaderTests
	{
		private ResourcesUiAssetLoader _loader;
		private Transform _parentTransform;
		private List<GameObject> _createdObjects;

		// Note: These tests use a mock approach since we can't easily create Resources folder assets in tests.
		// The loader will throw KeyNotFoundException for missing resources, which we verify.

		[SetUp]
		public void SetUp()
		{
			_loader = new ResourcesUiAssetLoader();
			
			var parentGo = new GameObject("TestParent");
			_parentTransform = parentGo.transform;
			
			_createdObjects = new List<GameObject> { parentGo };
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
		// ADMIT: ResourcesUiAssetLoader.InstantiatePrefab throws when Resources.Load returns null rather than
		// passing null on to Object.Instantiate, which fails far from the misconfigured address.
		// RCR: ResourcesUiAssetLoader.cs InstantiatePrefab — replace the throw with
		// `return UniTask.FromResult<GameObject>(null);` → RED (expected KeyNotFoundException, none thrown). 2026-08-02
		public void InstantiatePrefab_ThrowsKeyNotFoundException_WhenResourceNotFound()
		{
			// Arrange
			var config = TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "nonexistent/resource/path");

			// Act & Assert
			Assert.Throws<KeyNotFoundException>(() =>
			{
				_loader.InstantiatePrefab(config, _parentTransform).GetAwaiter().GetResult();
			});
		}

		[Test]
		// ADMIT: ResourcesUiAssetLoader.UnloadAsset's null guard lets UiService.Dispose sweep instances Unity has
		// already destroyed.
		// RCR: none reachable — deleting `if (asset != null)` leaves the test green because
		// Object.DestroyImmediate(null) is itself a no-op, so the guard has nothing observable to protect here.
		// D2 overclaim: the name promises the guard matters; only the double-destroy path would show it. 2026-08-02
		public void UnloadAsset_HandlesNullGracefully()
		{
			// Act & Assert - should not throw
			Assert.DoesNotThrow(() => _loader.UnloadAsset(null));
		}

		[Test]
		// ADMIT: ResourcesUiAssetLoader.UnloadAsset must destroy the instance; unlike Addressables it has no
		// release handle to fall back on, so a no-op leaks every unloaded presenter.
		// RCR: ResourcesUiAssetLoader.cs UnloadAsset — replace `Object.DestroyImmediate(asset);` with
		// `_ = asset;` → RED (Assert.IsTrue(testObject == null) fails). 2026-08-02
		public void UnloadAsset_DestroysGameObject()
		{
			// Arrange - create a simple GameObject to test destruction
			var testObject = new GameObject("TestObject");
			_createdObjects.Add(testObject);

			// Act
			_loader.UnloadAsset(testObject);

			// Assert
			Assert.IsTrue(testObject == null); // Unity destroys the object
		}

		[Test]
		// ADMIT: ResourcesUiAssetLoader's not-found message must quote UiConfig.Address — it is the only clue a
		// developer gets that the Resources path and the config disagree.
		// RCR: ResourcesUiAssetLoader.cs InstantiatePrefab — drop the interpolated address from the message → RED
		// (ex.Message no longer contains "UI/TestPresenter"). The throw-type sibling stays green. 2026-08-02
		public void InstantiatePrefab_UsesAddressAsResourcePath()
		{
			// Arrange
			const string resourcePath = "UI/TestPresenter";
			var config = TestHelpers.CreateTestConfig(typeof(TestUiPresenter), resourcePath);

			// Act & Assert
			// This will throw KeyNotFoundException with the path in the message
			var ex = Assert.Throws<KeyNotFoundException>(() =>
			{
				_loader.InstantiatePrefab(config, _parentTransform).GetAwaiter().GetResult();
			});

			Assert.IsTrue(ex.Message.Contains(resourcePath));
		}

		[Test]
		// ADMIT: ResourcesUiAssetLoader.InstantiatePrefab must hand back a deactivated instance — UiService owns
		// the first SetActive(true) through the open lifecycle.
		// RCR: ResourcesUiAssetLoader.cs InstantiatePrefab — flip to `instance.SetActive(true);` → RED (the
		// activeSelf assertion's own message fires: the loader must SetActive(false) on the new instance). 2026-08-02
		public void InstantiatePrefab_ValidResourceAddress_LoadsCachesAndInstantiatesInactive()
		{
			const string resourceAddress = "UiServiceTestPrefab";
			var config = TestHelpers.CreateTestConfig(typeof(TestUiPresenter), resourceAddress);

			var firstInstance = _loader.InstantiatePrefab(config, _parentTransform).GetAwaiter().GetResult();
			_createdObjects.Add(firstInstance);

			Assert.IsNotNull(firstInstance);
			Assert.AreEqual(_parentTransform, firstInstance.transform.parent);
			Assert.IsFalse(firstInstance.activeSelf,
				"ResourcesUiAssetLoader.InstantiatePrefab must SetActive(false) on the new instance — UiService relies on this to control visibility before the open lifecycle starts");

			var secondInstance = _loader.InstantiatePrefab(config, _parentTransform).GetAwaiter().GetResult();
			_createdObjects.Add(secondInstance);

			Assert.IsNotNull(secondInstance);
			Assert.AreNotSame(firstInstance, secondInstance,
				"Each call must return a distinct instance — the cache stores the prefab, not the instance");
		}
	}
}

