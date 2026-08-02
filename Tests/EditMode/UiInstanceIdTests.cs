using NUnit.Framework;
using System;

namespace GameLovers.UiService.Tests
{
    [TestFixture]
    public class UiInstanceIdTests
    {
        [Test]
        // ADMIT: UiInstanceId's constructor must keep a non-empty instance address verbatim — it is the key every
        // UiService lookup matches on, so losing it collapses all multi-instance presenters onto one id.
        // RCR: UiInstanceId.cs constructor — make the normalisation ternary yield `string.Empty` on both branches
        // → RED (expected "test_address", was ""). 2026-08-02
        public void Constructor_WithValidType_CreatesInstance()
        {
            // Arrange & Act
            var instanceId = new UiInstanceId(typeof(TestUiPresenter), "test_address");

            // Assert
            Assert.AreEqual(typeof(TestUiPresenter), instanceId.PresenterType);
            Assert.AreEqual("test_address", instanceId.InstanceAddress);
        }

        [Test]
        // ADMIT: UiInstanceId's constructor normalises an empty address to string.Empty; any other sentinel would
        // stop matching the default instance UiService.AddUi registers.
        // RCR: UiInstanceId.cs constructor — replace the ternary's `string.Empty` branch with `"__none__"` → RED
        // (expected String.Empty, was "__none__"). Leaves the non-empty-address sibling green. 2026-08-02
        public void Constructor_WithEmptyAddress_CreatesDefaultInstance()
        {
            // Arrange & Act
            var instanceId = new UiInstanceId(typeof(TestUiPresenter), string.Empty);

            // Assert
            Assert.AreEqual(string.Empty, instanceId.InstanceAddress);
            Assert.IsTrue(instanceId.IsDefault);
        }

        [Test]
        // ADMIT: UiInstanceId built from a null address must read as the default/singleton instance.
        // RCR: none exists — null trips both the constructor's `string.IsNullOrEmpty` normalisation and
        // IsDefault's own `string.IsNullOrEmpty`; disabling the normalisation leaves IsDefault(null) still true
        // (verified). Double-covered, not single-line falsifiable. 2026-08-02
        public void Constructor_WithNullAddress_CreatesDefaultInstance()
        {
            // Arrange & Act
            var instanceId = new UiInstanceId(typeof(TestUiPresenter), null);

            // Assert
            Assert.IsTrue(instanceId.IsDefault);
        }

        [Test]
        // ADMIT: UiInstanceId.Equals compares InstanceAddress by value; UiService stores ids in a List and every
        // visible/loaded lookup is a Contains, so a broken address comparison loses presenters silently.
        // RCR: UiInstanceId.cs Equals — invert the address comparison to `InstanceAddress !=
        // other.InstanceAddress` → RED (Assert.IsTrue(id1.Equals(id2)) fails). 2026-08-02
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
        // ADMIT: UiInstanceId.Equals must compare PresenterType, or two different presenters sharing an instance
        // address become the same id and close/unload hit the wrong one.
        // RCR: UiInstanceId.cs Equals — replace `PresenterType == other.PresenterType` with `true` → RED
        // (Assert.IsFalse(id1.Equals(id2)) fails). 2026-08-02
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
        // ADMIT: UiInstanceId.Equals must compare InstanceAddress, or two instances of the same presenter type at
        // different addresses collapse into one id.
        // RCR: UiInstanceId.cs Equals — replace `InstanceAddress == other.InstanceAddress` with `true` → RED
        // (Assert.IsFalse(id1.Equals(id2)) fails). 2026-08-02
        public void Equals_DifferentAddress_ReturnsFalse()
        {
            // Arrange
            var id1 = new UiInstanceId(typeof(TestUiPresenter), "address1");
            var id2 = new UiInstanceId(typeof(TestUiPresenter), "address2");

            // Assert
            Assert.IsFalse(id1.Equals(id2));
        }

        [Test]
        // ADMIT: UiInstanceId.GetHashCode must fold BOTH PresenterType and InstanceAddress, or ids differing
        // only in address share a bucket in every hash-based lookup keyed on this struct.
        // RCR: UiInstanceId.cs GetHashCode — drop the `^ (InstanceAddress != null ? ...)` term → RED (the
        // same-type/different-address pair below hashes equal). Asserting only that identical ids agree would
        // stay green even for `return 0;`, which is why the discriminating pairs are the point of this test.
        public void GetHashCode_FoldsBothTypeAndAddress()
        {
            // Arrange
            var id1 = new UiInstanceId(typeof(TestUiPresenter), "address");
            var id2 = new UiInstanceId(typeof(TestUiPresenter), "address");
            var differentAddress = new UiInstanceId(typeof(TestUiPresenter), "other_address");
            var differentType = new UiInstanceId(typeof(TestDataUiPresenter), "address");

            // Assert
            Assert.AreEqual(id1.GetHashCode(), id2.GetHashCode(), "structurally identical ids must agree");
            Assert.AreNotEqual(id1.GetHashCode(), differentAddress.GetHashCode(), "address must participate");
            Assert.AreNotEqual(id1.GetHashCode(), differentType.GetHashCode(), "presenter type must participate");
        }

        [Test]
        // ADMIT: UiInstanceId.ToString is the only identification in every UiService warning/exception message
        // ("already loaded", "is not open"); dropping the address makes multi-instance diagnostics ambiguous.
        // RCR: UiInstanceId.cs ToString — drop `:{InstanceAddress}` from the non-default branch → RED
        // (Assert.IsTrue(result.Contains("my_instance")) fails). 2026-08-02
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

        [Test]
        // ADMIT: UiInstanceId.Default must construct with no instance address, or the singleton factory produces
        // an id that never matches what UiService.AddUi<T>(ui, layer) registers.
        // RCR: UiInstanceId.cs Default — pass an address: `new UiInstanceId(presenterType, "default")` → RED
        // (expected String.Empty, was "default"; IsDefault also False). 2026-08-02
        public void Default_ProducesIsDefaultTrueAndEmptyAddress()
        {
            var instanceId = UiInstanceId.Default(typeof(TestUiPresenter));

            Assert.AreEqual(typeof(TestUiPresenter), instanceId.PresenterType);
            Assert.AreEqual(string.Empty, instanceId.InstanceAddress);
            Assert.IsTrue(instanceId.IsDefault);
        }

        [Test]
        // ADMIT: UiInstanceId.Named must forward its instanceAddress argument, or every named instance silently
        // becomes the default one.
        // RCR: UiInstanceId.cs Named — drop the argument: `new UiInstanceId(presenterType)` → RED
        // (expected "popup_1", was ""). 2026-08-02
        public void Named_NonEmptyAddress_ProducesIsDefaultFalse()
        {
            var instanceId = UiInstanceId.Named(typeof(TestUiPresenter), "popup_1");

            Assert.AreEqual(typeof(TestUiPresenter), instanceId.PresenterType);
            Assert.AreEqual("popup_1", instanceId.InstanceAddress);
            Assert.IsFalse(instanceId.IsDefault);
        }
    }
}

