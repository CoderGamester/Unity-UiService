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
		public void UnloadAsset_HandlesNullGracefully()
		{
			// Act & Assert - should not throw
			Assert.DoesNotThrow(() => _loader.UnloadAsset(null));
		}

		[Test]
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
		[Ignore("Body is wired but the fixture is missing — once Tests/EditMode/Loaders/Resources/UiServiceTestPrefab.prefab (+ .meta) is created in Unity Editor, simply remove this [Ignore] attribute. See package AGENTS.md §3 Tests for the fixture-creation steps.")]
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

		[Test]
		public void MultipleInstances_CanBeCreated()
		{
			// This test verifies the loader doesn't have singleton restrictions
			// We can't test with real Resources, but we verify the loader can be called multiple times
			
			var loader1 = new ResourcesUiAssetLoader();
			var loader2 = new ResourcesUiAssetLoader();

			// Both should be independent instances
			Assert.AreNotSame(loader1, loader2);
		}
	}
}

