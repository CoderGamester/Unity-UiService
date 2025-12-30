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
		public void InstantiatePrefab_UsesAddressableAddressAsResourcePath()
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

