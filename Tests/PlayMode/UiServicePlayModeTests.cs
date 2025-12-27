using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.TestTools;

namespace GameLovers.UiService.Tests.PlayMode
{
    /// <summary>
    /// Core PlayMode tests for UiService that require Init() to be called.
    /// These tests need PlayMode because UiService.Init() calls DontDestroyOnLoad
    /// which only works during runtime.
    /// </summary>
    [TestFixture]
    public class UiServiceCorePlayModeTests
    {
        private MockAssetLoader _mockLoader;
        private UiService _service;

        [SetUp]
        public void Setup()
        {
            _mockLoader = new MockAssetLoader();
        }

        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();
            _mockLoader?.Cleanup();
        }

        [UnityTest]
        public IEnumerator Init_WithValidConfigs_InitializesCorrectly()
        {
            // Arrange
            _service = new UiService(_mockLoader);
            var configs = TestHelpers.CreateTestConfigs(
                TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "test_address", 0)
            );

            // Act
            _service.Init(configs);

            // Assert - Check that service can accept load requests
            Assert.IsNotNull(_service.UiSets);
            Assert.IsNotNull(_service.VisiblePresenters);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AddUiConfig_NewConfig_AddsSuccessfully()
        {
            // Arrange
            _service = new UiService(_mockLoader);
            _service.Init(TestHelpers.CreateTestConfigs());
            var config = TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "new_address", 0);

            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => _service.AddUiConfig(config));
            yield return null;
        }

        [UnityTest]
        public IEnumerator AddUiSet_NewSet_AddsSuccessfully()
        {
            // Arrange
            _service = new UiService(_mockLoader);
            _service.Init(TestHelpers.CreateTestConfigs());
            var set = TestHelpers.CreateTestUiSet(1);

            // Act & Assert
            Assert.DoesNotThrow(() => _service.AddUiSet(set));
            Assert.IsTrue(_service.UiSets.ContainsKey(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator VisiblePresenters_InitiallyEmpty()
        {
            // Arrange
            _service = new UiService(_mockLoader);
            _service.Init(TestHelpers.CreateTestConfigs());

            // Assert
            Assert.AreEqual(0, _service.VisiblePresenters.Count);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GetLoadedPresenters_InitiallyEmpty()
        {
            // Arrange
            _service = new UiService(_mockLoader);
            _service.Init(TestHelpers.CreateTestConfigs());

            // Act
            var loaded = _service.GetLoadedPresenters();

            // Assert
            Assert.AreEqual(0, loaded.Count);
            yield return null;
        }

        [UnityTest]
        public IEnumerator GetUi_NotLoaded_ThrowsKeyNotFoundException()
        {
            // Arrange
            _service = new UiService(_mockLoader);
            var configs = TestHelpers.CreateTestConfigs(
                TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "test_address", 0)
            );
            _service.Init(configs);

            // Act & Assert
            Assert.Throws<KeyNotFoundException>(() => _service.GetUi<TestUiPresenter>());
            yield return null;
        }

        [UnityTest]
        public IEnumerator UnloadUi_NotLoaded_ThrowsKeyNotFoundException()
        {
            // Arrange
            _service = new UiService(_mockLoader);
            var configs = TestHelpers.CreateTestConfigs(
                TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "test_address", 0)
            );
            _service.Init(configs);

            // Act & Assert
            Assert.Throws<KeyNotFoundException>(() => _service.UnloadUi(typeof(TestUiPresenter)));
            yield return null;
        }

        [UnityTest]
        public IEnumerator RemoveUiSet_InvalidSetId_ThrowsKeyNotFoundException()
        {
            // Arrange
            _service = new UiService(_mockLoader);
            _service.Init(TestHelpers.CreateTestConfigs());

            // Act & Assert
            Assert.Throws<KeyNotFoundException>(() => _service.RemoveUiSet(999));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Dispose_CalledTwice_DoesNotThrow()
        {
            // Arrange
            _service = new UiService(_mockLoader);
            _service.Init(TestHelpers.CreateTestConfigs());

            // Act & Assert
            Assert.DoesNotThrow(() => _service.Dispose());
            Assert.DoesNotThrow(() => _service.Dispose());
            yield return null;
        }

        [UnityTest]
        public IEnumerator RemoveUi_NotLoaded_ReturnsFalse()
        {
            // Arrange
            _service = new UiService(_mockLoader);
            _service.Init(TestHelpers.CreateTestConfigs());

            // Act
            var result = _service.RemoveUi(typeof(TestUiPresenter));

            // Assert
            Assert.IsFalse(result);
            yield return null;
        }

        [UnityTest]
        public IEnumerator IsVisible_NotLoaded_ReturnsFalse()
        {
            // Arrange
            _service = new UiService(_mockLoader);
            var configs = TestHelpers.CreateTestConfigs(
                TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "test_address", 0)
            );
            _service.Init(configs);

            // Act
            var result = _service.IsVisible<TestUiPresenter>();

            // Assert
            Assert.IsFalse(result);
            yield return null;
        }
    }
}

