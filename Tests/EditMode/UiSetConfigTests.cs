using NUnit.Framework;
using System.Collections.Generic;

namespace GameLovers.UiService.Tests
{
    [TestFixture]
    public class UiSetConfigTests
    {
        [Test]
        // ADMIT: UiSetConfig.SetId is the key UiService.AddUiSet/_uiSets is indexed by.
        // RCR: none exists — UiSetConfig is a field-only struct with no logic; the only edit that reddens this is
        // deleting the field, which is a compile error. The id's real behaviour is pinned by
        // Serialization_RoundTrip_PreservesData and UiServiceCorePlayModeTests.AddUiSet_NewSet_AddsSuccessfully. 2026-08-02
        public void UiSetConfig_Creation_StoresSetId()
        {
            // Arrange & Act
            var setConfig = new UiSetConfig
            {
                SetId = 42,
                UiInstanceIds = new UiInstanceId[0]
            };

            // Assert
            Assert.AreEqual(42, setConfig.SetId);
        }

        [Test]
        // ADMIT: UiSetEntry.ToUiInstanceId must carry InstanceAddress into the resulting id — a set entry that
        // loses its address resolves to the default instance and UiService opens the wrong one.
        // RCR: UiSetConfig.cs UiSetEntry.ToUiInstanceId — pass `null` instead of the address ternary → RED
        // (expected "test_instance", was ""). 2026-08-02
        public void UiSetEntry_ToUiInstanceId_ConvertsCorrectly()
        {
            // Arrange
            var entry = new UiSetEntry
            {
                UiTypeName = typeof(TestUiPresenter).AssemblyQualifiedName,
                InstanceAddress = "test_instance"
            };

            // Act - Access the UiInstanceId property which does the conversion
            var instanceId = entry.ToUiInstanceId();

            // Assert
            Assert.AreEqual(typeof(TestUiPresenter), instanceId.PresenterType);
            Assert.AreEqual("test_instance", instanceId.InstanceAddress);
        }

        [Test]
        // ADMIT: UiSetEntry.ToUiInstanceId normalises an empty serialized address to the default instance; any
        // other sentinel makes an unnamed set entry stop matching the presenter UiService loaded.
        // RCR: UiSetConfig.cs UiSetEntry.ToUiInstanceId — replace the ternary's `null` branch with `"__set__"` →
        // RED (Assert.IsTrue(instanceId.IsDefault) fails). Leaves the named-address sibling green. 2026-08-02
        public void UiSetEntry_EmptyAddress_IsDefault()
        {
            // Arrange
            var entry = new UiSetEntry
            {
                UiTypeName = typeof(TestUiPresenter).AssemblyQualifiedName,
                InstanceAddress = ""
            };

            // Act
            var instanceId = entry.ToUiInstanceId();

            // Assert
            Assert.IsTrue(instanceId.IsDefault);
        }

        [Test]
        // ADMIT: UiSetEntry.FromUiInstanceId must serialize the instance address, or a multi-instance set entry
        // deserializes onto the default instance after a domain reload.
        // RCR: UiSetConfig.cs UiSetEntry.FromUiInstanceId — replace the address with `string.Empty` → RED
        // (deserialized[0] expected TestUiPresenter:inst1, was TestUiPresenter). 2026-08-02
        public void Serialization_RoundTrip_PreservesData()
        {
            // Arrange
            var originalConfig = new UiSetConfig
            {
                SetId = 1,
                UiInstanceIds = new[]
                {
                    new UiInstanceId(typeof(TestUiPresenter), "inst1"),
                    new UiInstanceId(typeof(TestDataUiPresenter))
                }
            };

            // Act
            var serializable = UiSetConfigSerializable.FromUiSetConfig(originalConfig);
            var deserialized = UiSetConfigSerializable.ToUiSetConfig(serializable);

            // Assert
            Assert.AreEqual(originalConfig.SetId, deserialized.SetId);
            Assert.AreEqual(originalConfig.UiInstanceIds.Length, deserialized.UiInstanceIds.Length);
            Assert.AreEqual(originalConfig.UiInstanceIds[0], deserialized.UiInstanceIds[0]);
            Assert.AreEqual(originalConfig.UiInstanceIds[1], deserialized.UiInstanceIds[1]);
        }
    }
}

