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
    }
}

