using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
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
        private UnityAction<Vector2, Vector2> _resolutionListener;
        private UnityAction<DeviceOrientation, DeviceOrientation> _orientationListener;

        [SetUp]
        public void Setup()
        {
            _mockLoader = new MockAssetLoader();
        }

        [TearDown]
        public void TearDown()
        {
            if (_resolutionListener != null)
            {
                UiService.OnResolutionChanged.RemoveListener(_resolutionListener);
                _resolutionListener = null;
            }
            if (_orientationListener != null)
            {
                UiService.OnOrientationChanged.RemoveListener(_orientationListener);
                _orientationListener = null;
            }
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

        [UnityTest]
        public IEnumerator OnResolutionChanged_OnResolutionShift_FiresWithNewResolution()
        {
            var fired = 0;
            Vector2 receivedFrom = Vector2.zero;
            Vector2 receivedTo = Vector2.zero;

            _resolutionListener = (from, to) =>
            {
                fired++;
                receivedFrom = from;
                receivedTo = to;
            };
            UiService.OnResolutionChanged.AddListener(_resolutionListener);

            var prev = new Vector2(1280, 720);
            var next = new Vector2(1920, 1080);
            UiService.OnResolutionChanged.Invoke(prev, next);

            Assert.AreEqual(1, fired);
            Assert.AreEqual(prev, receivedFrom);
            Assert.AreEqual(next, receivedTo);
            yield return null;
        }

        [UnityTest]
        public IEnumerator OnOrientationChanged_OnOrientationShift_FiresWithNewOrientation()
        {
            var fired = 0;
            DeviceOrientation receivedFrom = DeviceOrientation.Unknown;
            DeviceOrientation receivedTo = DeviceOrientation.Unknown;

            _orientationListener = (from, to) =>
            {
                fired++;
                receivedFrom = from;
                receivedTo = to;
            };
            UiService.OnOrientationChanged.AddListener(_orientationListener);

            UiService.OnOrientationChanged.Invoke(DeviceOrientation.Portrait, DeviceOrientation.LandscapeLeft);

            Assert.AreEqual(1, fired);
            Assert.AreEqual(DeviceOrientation.Portrait, receivedFrom);
            Assert.AreEqual(DeviceOrientation.LandscapeLeft, receivedTo);
            yield return null;
        }

        [UnityTest]
        public IEnumerator IsOpen_ReflectsActiveSelfState_AcrossOpenClose()
        {
            _mockLoader.RegisterPrefab<TestUiPresenter>("isopen_target");
            _service = new UiService(_mockLoader);
            _service.Init(TestHelpers.CreateTestConfigs(
                TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "isopen_target", 0)));

            var loadTask = _service.LoadUiAsync(typeof(TestUiPresenter), openAfter: false);
            yield return loadTask.ToCoroutine();
            var presenter = loadTask.GetAwaiter().GetResult() as TestUiPresenter;
            Assert.IsFalse(presenter.IsOpen);

            var openTask = _service.OpenUiAsync(typeof(TestUiPresenter));
            yield return openTask.ToCoroutine();
            yield return presenter.OpenTransitionTask.ToCoroutine();
            Assert.IsTrue(presenter.IsOpen);

            _service.CloseUi(typeof(TestUiPresenter));
            yield return presenter.CloseTransitionTask.ToCoroutine();
            Assert.IsFalse(presenter.IsOpen);
        }

        [UnityTest]
        public IEnumerator AddUi_RegistersInstance_AppearsInGetUi()
        {
            _service = new UiService(_mockLoader);
            _service.Init(TestHelpers.CreateTestConfigs(
                TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "addui_target", 0)));

            var prefab = TestHelpers.CreateTestPresenterPrefab<TestUiPresenter>();
            var presenter = prefab.GetComponent<TestUiPresenter>();

            _service.AddUi(presenter, 0);

            Assert.AreSame(presenter, _service.GetUi<TestUiPresenter>());
            Assert.IsFalse(_service.IsVisible<TestUiPresenter>());

            UnityEngine.Object.DestroyImmediate(prefab);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AddUi_MultiInstanceWithAddress_RegistersUnderInstanceId()
        {
            _service = new UiService(_mockLoader);
            _service.Init(TestHelpers.CreateTestConfigs(
                TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "multi_addui", 0)));

            var prefabA = TestHelpers.CreateTestPresenterPrefab<TestUiPresenter>("instA");
            var prefabB = TestHelpers.CreateTestPresenterPrefab<TestUiPresenter>("instB");
            var presenterA = prefabA.GetComponent<TestUiPresenter>();
            var presenterB = prefabB.GetComponent<TestUiPresenter>();

            _service.AddUi(presenterA, 0, "instance_a");
            _service.AddUi(presenterB, 0, "instance_b");

            Assert.AreSame(presenterA, _service.GetUi<TestUiPresenter>("instance_a"));
            Assert.AreSame(presenterB, _service.GetUi<TestUiPresenter>("instance_b"));
            Assert.AreNotSame(presenterA, presenterB);

            UnityEngine.Object.DestroyImmediate(prefabA);
            UnityEngine.Object.DestroyImmediate(prefabB);
            yield return null;
        }
    }
}

