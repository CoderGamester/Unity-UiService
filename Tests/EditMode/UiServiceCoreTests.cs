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
        // ADMIT: UiService's constructor must store the injected IUiAssetLoader; reverting to an always-default
        // AddressablesUiAssetLoader would make LoadUiAsync bypass the injected loader entirely.
        // RCR: UiService.cs ctor — replace `_assetLoader = assetLoader;` with
        // `_assetLoader = new AddressablesUiAssetLoader();` → RED (_mockLoader.InstantiateCallCount stays 0). 2026-08-01
        public void Constructor_WithCustomAssetLoader_UsesCustomLoader()
        {
            // Arrange
            _mockLoader.RegisterPrefab<TestUiPresenter>("custom_loader_address");
            var config = TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "custom_loader_address");

            // Act
            // Note: Init() is deliberately not called here (it calls DontDestroyOnLoad, which is
            // PlayMode-only per this fixture's header comment). AddUiConfig + LoadUiAsync route
            // through the injected loader without needing Init(), same pattern already used by
            // Tests/EditMode/Loaders/PrefabRegistryUiAssetLoaderTests.cs.
            _service = new UiService(_mockLoader);
            _service.AddUiConfig(config);
            _service.LoadUiAsync(typeof(TestUiPresenter)).GetAwaiter().GetResult();

            // Assert
            Assert.AreEqual(1, _mockLoader.InstantiateCallCount,
                "UiService must route LoadUiAsync through the IUiAssetLoader passed into its constructor, not a default loader");
        }

        [Test]
        // ADMIT: UiService.Init's null guard turns a missing UiConfigs asset reference into a named
        // ArgumentNullException instead of an opaque NullReferenceException deep in the config loop.
        // RCR: UiService.cs Init — disable the guard with `if (false)` → RED (Assert.Throws reports
        // NullReferenceException on `configs.Configs` where ArgumentNullException was expected). 2026-08-02
        public void Init_WithNullConfigs_ThrowsArgumentNullException()
        {
            // Arrange
            _service = new UiService(_mockLoader);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.Init(null));
        }
    }
}
