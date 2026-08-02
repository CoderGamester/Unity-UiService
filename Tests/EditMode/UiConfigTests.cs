using NUnit.Framework;
using UnityEngine;

namespace GameLovers.UiService.Tests
{
    [TestFixture]
    public class UiConfigTests
    {
        [Test]
        // ADMIT: UiConfig is the shape UiService.Init and every loader read; this pins its four public fields.
        // RCR: none exists — UiConfig is a field-only struct with no logic, so the only edit that reddens this is
        // deleting or renaming a field, which is a compile error, not a mutation (verified: every operator in
        // UiConfigs.UiConfigSerializable can be broken with this test still green). 2026-08-02
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
        // ADMIT: a default-constructed UiConfig must read as unconfigured so UiService.Init's empty-address guard
        // fires rather than a presenter silently loading from address null.
        // RCR: none exists — the asserted values are C#'s zero-init for a field-only struct; no line in Runtime/
        // participates, so every candidate edit is a compile error. Kept as the anchor for the guard above. 2026-08-02
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
        // ADMIT: UiConfigs.Configs round-trips through UiConfigSerializable, and LoadSynchronously is the field
        // most easily dropped there — AddressablesUiAssetLoader reads it to pick sync instantiation.
        // RCR: UiConfigs.cs UiConfigSerializable(UiConfig) operator — replace `LoadSynchronously =
        // config.LoadSynchronously` with `config.LoadSynchronously && config.Layer != 0` → RED (expected True,
        // was False). Layer-gated so the ImplicitConversion sibling (Layer 7) stays green. 2026-08-02
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
        // ADMIT: UiConfigs.UiConfigSerializable's UiConfig→serializable operator must carry Layer across, or every
        // authored sorting layer collapses to 0 on save.
        // RCR: UiConfigs.cs UiConfigSerializable(UiConfig) operator — replace `Layer = config.Layer,` with
        // `Layer = 0,` → RED (expected 7, was 0). 2026-08-02
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
        // ADMIT: UiConfigs.UiConfigSerializable's serializable→UiConfig operator must rehydrate Layer, or every
        // presenter loaded from an asset gets Canvas.sortingOrder 0 regardless of what was authored.
        // RCR: UiConfigs.cs UiConfig(UiConfigSerializable) operator — replace `Layer = serializable.Layer,` with
        // `Layer = 0,` → RED (expected 3, was 0). 2026-08-02
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
        // ADMIT: UiConfigs.SetSetsSize drops set entries whose UiTypeName no longer matches any config; without it
        // a renamed or deleted presenter type stays in the set and UiService loads a null type at runtime.
        // RCR: UiConfigs.cs SetSetsSize — neuter the filter to `set.UiEntries.RemoveAll(entry => false);` → RED
        // (expected 1 surviving entry, was 2). 2026-08-02
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

