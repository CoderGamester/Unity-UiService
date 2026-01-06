using NUnit.Framework;
using System.Collections.Generic;

namespace GameLovers.UiService.Tests
{
    [TestFixture]
    public class UiSetConfigTests
    {
        [Test]
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

