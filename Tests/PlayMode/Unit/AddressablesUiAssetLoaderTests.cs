using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace GameLovers.UiService.Tests.PlayMode
{
	[TestFixture]
	public class AddressablesUiAssetLoaderTests
	{
		// Tests in this fixture require an Addressables build/group containing the address below.
		// They are [Ignore]'d until the host project ships such a fixture.
		private const string KnownAddressableAddress = "tests/AddressablesUiAssetLoader/TestPresenterPrefab";

		private AddressablesUiAssetLoader _loader;
		private Transform _parent;

		[SetUp]
		public void SetUp()
		{
			_loader = new AddressablesUiAssetLoader();
			_parent = new GameObject("AddressablesTestParent").transform;
		}

		[TearDown]
		public void TearDown()
		{
			if (_parent != null)
			{
				Object.DestroyImmediate(_parent.gameObject);
			}
		}

		[UnityTest, Ignore("Requires an Addressables build containing the fixture address; un-ignore when the host project ships one.")]
		public IEnumerator InstantiatePrefab_ValidAddress_ReturnsInstanceFromHandle()
		{
			var config = new UiConfig
			{
				Address = KnownAddressableAddress,
				Layer = 0,
				UiType = typeof(TestUiPresenter)
			};

			var task = _loader.InstantiatePrefab(config, _parent);
			yield return task.ToCoroutine();
			var instance = task.GetAwaiter().GetResult();

			Assert.IsNotNull(instance);
			Assert.AreEqual(_parent, instance.transform.parent);

			_loader.UnloadAsset(instance);
		}

		[UnityTest, Ignore("Requires an Addressables build containing the fixture address; un-ignore when the host project ships one.")]
		public IEnumerator UnloadAsset_AfterInstantiate_ReleasesAddressableHandle()
		{
			var config = new UiConfig
			{
				Address = KnownAddressableAddress,
				Layer = 0,
				UiType = typeof(TestUiPresenter)
			};
			var task = _loader.InstantiatePrefab(config, _parent);
			yield return task.ToCoroutine();
			var instance = task.GetAwaiter().GetResult();
			Assert.IsNotNull(instance);

			_loader.UnloadAsset(instance);
			yield return null;

			Assert.IsTrue(instance == null);
		}
	}
}
