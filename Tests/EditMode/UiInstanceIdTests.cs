using NUnit.Framework;
using System;

namespace GameLovers.UiService.Tests
{
    [TestFixture]
    public class UiInstanceIdTests
    {
        [Test]
        public void Constructor_WithValidType_CreatesInstance()
        {
            // Arrange & Act
            var instanceId = new UiInstanceId(typeof(TestUiPresenter), "test_address");

            // Assert
            Assert.AreEqual(typeof(TestUiPresenter), instanceId.PresenterType);
            Assert.AreEqual("test_address", instanceId.InstanceAddress);
        }

        [Test]
        public void Constructor_WithEmptyAddress_CreatesDefaultInstance()
        {
            // Arrange & Act
            var instanceId = new UiInstanceId(typeof(TestUiPresenter), string.Empty);

            // Assert
            Assert.AreEqual(string.Empty, instanceId.InstanceAddress);
            Assert.IsTrue(instanceId.IsDefault);
        }

        [Test]
        public void Constructor_WithNullAddress_CreatesDefaultInstance()
        {
            // Arrange & Act
            var instanceId = new UiInstanceId(typeof(TestUiPresenter), null);

            // Assert
            Assert.IsTrue(instanceId.IsDefault);
        }

        [Test]
        public void Equals_SameTypeAndAddress_ReturnsTrue()
        {
            // Arrange
            var id1 = new UiInstanceId(typeof(TestUiPresenter), "address");
            var id2 = new UiInstanceId(typeof(TestUiPresenter), "address");

            // Assert
            Assert.IsTrue(id1.Equals(id2));
            Assert.IsTrue(id1 == id2);
        }

        [Test]
        public void Equals_DifferentType_ReturnsFalse()
        {
            // Arrange
            var id1 = new UiInstanceId(typeof(TestUiPresenter), "address");
            var id2 = new UiInstanceId(typeof(TestDataUiPresenter), "address");

            // Assert
            Assert.IsFalse(id1.Equals(id2));
            Assert.IsTrue(id1 != id2);
        }

        [Test]
        public void Equals_DifferentAddress_ReturnsFalse()
        {
            // Arrange
            var id1 = new UiInstanceId(typeof(TestUiPresenter), "address1");
            var id2 = new UiInstanceId(typeof(TestUiPresenter), "address2");

            // Assert
            Assert.IsFalse(id1.Equals(id2));
        }

        [Test]
        public void GetHashCode_SameTypeAndAddress_ReturnsSameHash()
        {
            // Arrange
            var id1 = new UiInstanceId(typeof(TestUiPresenter), "address");
            var id2 = new UiInstanceId(typeof(TestUiPresenter), "address");

            // Assert
            Assert.AreEqual(id1.GetHashCode(), id2.GetHashCode());
        }

        [Test]
        public void ToString_ReturnsExpectedFormat()
        {
            // Arrange
            var instanceId = new UiInstanceId(typeof(TestUiPresenter), "my_instance");

            // Act
            var result = instanceId.ToString();

            // Assert
            Assert.IsTrue(result.Contains("TestUiPresenter"));
            Assert.IsTrue(result.Contains("my_instance"));
        }
    }
}

