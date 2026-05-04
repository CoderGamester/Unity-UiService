using NUnit.Framework;
using UnityEngine;

namespace GameLovers.UiService.Tests
{
    [TestFixture]
    public class UiConfigTests
    {
        [Test]
        public void UiConfig_Creation_PreservesAllProperties()
        {
            // Arrange & Act
            var config = new UiConfig
            {
                Address = "test_address",
                Layer = 5,
                UiType = typeof(TestUiPresenter),
                LoadSynchronously = true
            };

            // Assert
            Assert.AreEqual("test_address", config.Address);
            Assert.AreEqual(5, config.Layer);
            Assert.AreEqual(typeof(TestUiPresenter), config.UiType);
            Assert.IsTrue(config.LoadSynchronously);
        }

        [Test]
        public void UiConfig_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var config = new UiConfig();

            // Assert
            Assert.IsNull(config.Address);
            Assert.AreEqual(0, config.Layer);
            Assert.IsNull(config.UiType);
            Assert.IsFalse(config.LoadSynchronously);
        }

        [Test]
        public void ConfigsSetter_RoundTrip_PreservesLoadSynchronously()
        {
            var configs = ScriptableObject.CreateInstance<PrefabRegistryUiConfigs>();
            configs.Configs = new System.Collections.Generic.List<UiConfig>
            {
                new UiConfig { Address = "a", Layer = 0, UiType = typeof(TestUiPresenter), LoadSynchronously = true }
            };

            var roundTripped = configs.Configs;

            Assert.AreEqual(1, roundTripped.Count);
            Assert.IsTrue(roundTripped[0].LoadSynchronously);

            ScriptableObject.DestroyImmediate(configs);
        }

        [Test]
        public void ImplicitConversion_ConfigToSerializable_PreservesAddressLayerTypeAndLoadSynchronously()
        {
            var config = new UiConfig
            {
                Address = "addr_42",
                Layer = 7,
                UiType = typeof(TestUiPresenter),
                LoadSynchronously = true
            };

            UiConfigs.UiConfigSerializable serializable = config;

            Assert.AreEqual("addr_42", serializable.Address);
            Assert.AreEqual(7, serializable.Layer);
            Assert.AreEqual(typeof(TestUiPresenter).AssemblyQualifiedName, serializable.UiType);
            Assert.IsTrue(serializable.LoadSynchronously);
        }

        [Test]
        public void ImplicitConversion_SerializableToConfig_RehydratesAddressLayerTypeAndLoadSynchronously()
        {
            var serializable = new UiConfigs.UiConfigSerializable
            {
                Address = "addr_99",
                Layer = 3,
                UiType = typeof(TestUiPresenter).AssemblyQualifiedName,
                LoadSynchronously = true
            };

            UiConfig config = serializable;

            Assert.AreEqual("addr_99", config.Address);
            Assert.AreEqual(3, config.Layer);
            Assert.AreEqual(typeof(TestUiPresenter), config.UiType);
            Assert.IsTrue(config.LoadSynchronously);
        }

        [Test]
        public void SetSetsSize_ShrinkAndGrow_ResizesAndDropsInvalidEntries()
        {
            var configs = ScriptableObject.CreateInstance<PrefabRegistryUiConfigs>();
            try
            {
                configs.Configs = new System.Collections.Generic.List<UiConfig>
                {
                    new UiConfig { Address = "a", Layer = 0, UiType = typeof(TestUiPresenter), LoadSynchronously = false }
                };

                var validId = new UiInstanceId(typeof(TestUiPresenter), "a");
                var staleId = new UiInstanceId(typeof(UiPresenter), "stale");

                var preSets = new[]
                {
                    new UiSetConfig { SetId = 0, UiInstanceIds = new[] { validId, staleId } },
                    new UiSetConfig { SetId = 1, UiInstanceIds = new UiInstanceId[0] },
                    new UiSetConfig { SetId = 2, UiInstanceIds = new UiInstanceId[0] }
                };
                configs.SetSets(preSets);
                Assume.That(configs.Sets.Count, Is.EqualTo(3));

                configs.SetSetsSize(2);
                Assert.AreEqual(2, configs.Sets.Count, "Shrink should drop the trailing set");

                var set0AfterShrink = configs.Sets[0];
                Assert.AreEqual(1, set0AfterShrink.UiInstanceIds.Length,
                    "Stale entry whose UiTypeName references no config should be filtered out");
                Assert.AreEqual(typeof(TestUiPresenter), set0AfterShrink.UiInstanceIds[0].PresenterType);

                configs.SetSetsSize(4);
                Assert.AreEqual(4, configs.Sets.Count, "Grow should append empty sets at indices 2 and 3");
                Assert.AreEqual(0, configs.Sets[2].UiInstanceIds.Length);
                Assert.AreEqual(0, configs.Sets[3].UiInstanceIds.Length);
            }
            finally
            {
                ScriptableObject.DestroyImmediate(configs);
            }
        }
    }
}

