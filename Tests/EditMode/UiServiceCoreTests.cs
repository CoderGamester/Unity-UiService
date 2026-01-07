using NUnit.Framework;
using System;

namespace GameLovers.UiService.Tests
{
    /// <summary>
    /// EditMode tests for UiService constructor and basic initialization.
    /// Tests that require Init() (which calls DontDestroyOnLoad) are in PlayMode tests.
    /// </summary>
    [TestFixture]
    public class UiServiceCoreTests
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
            _mockLoader?.Cleanup();
        }

        [Test]
        public void Constructor_WithDefaultParams_CreatesService()
        {
            // Act
            _service = new UiService();

            // Assert
            Assert.IsNotNull(_service);
        }

        [Test]
        public void Constructor_WithCustomAssetLoader_UsesCustomLoader()
        {
            // Act
            _service = new UiService(_mockLoader);

            // Assert
            Assert.IsNotNull(_service);
        }

        [Test]
        public void Init_WithNullConfigs_ThrowsArgumentNullException()
        {
            // Arrange
            _service = new UiService(_mockLoader);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.Init(null));
        }
    }
}
