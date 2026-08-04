using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
        // ADMIT: UiService.Init must copy the asset's sets into _uiSets and its configs into _uiConfigs; the
        // ReadOnly wrappers are built by the constructor and say nothing about Init.
        // RCR: UiService.cs Init - replace `AddUiSet(set);` inside the `foreach (var set in sets)` loop with
        // `_ = set;` -> RED (UiSets.Count expected 1, was 0). 2026-08-04
        public IEnumerator Init_WithValidConfigs_InitializesCorrectly()
        {
            // Arrange
            _service = new UiService(_mockLoader);
            var configs = TestHelpers.CreateTestConfigsWithSets(
                new[] { TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "test_address", 0) },
                new[] { TestHelpers.CreateTestUiSet(7, new UiInstanceId(typeof(TestUiPresenter), "test_address")) }
            );

            // Act
            _service.Init(configs);

            // Assert - the sets half
            Assert.AreEqual(1, _service.UiSets.Count);
            Assert.IsTrue(_service.UiSets.ContainsKey(7));

            // Assert - the configs half: a second add of the same type only warns if Init registered it.
            LogAssert.Expect(LogType.Warning, new Regex("was already added"));
            _service.AddUiConfig(TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "test_address", 0));
            yield return null;
        }

        [UnityTest]
        // ADMIT: UiService.AddUiConfig must actually store the config - the TryAdd both stores it and reports
        // the duplicate, so a guard that only checks membership registers nothing.
        // RCR: UiService.cs AddUiConfig - replace `if (!_uiConfigs.TryAdd(config.UiType, config))` with
        // `if (_uiConfigs.ContainsKey(config.UiType))` -> RED (LoadUiAsync throws KeyNotFoundException). 2026-08-04
        public IEnumerator AddUiConfig_NewConfig_AddsSuccessfully()
        {
            // Arrange
            _mockLoader.RegisterPrefab<TestUiPresenter>("new_address");
            _service = new UiService(_mockLoader);
            _service.Init(TestHelpers.CreateTestConfigs());
            var config = TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "new_address", 0);

            // Act
            _service.AddUiConfig(config);

            // Assert - registration is only observable through a load that would otherwise throw
            var task = _service.LoadUiAsync(typeof(TestUiPresenter));
            yield return task.ToCoroutine();
            var presenter = task.GetAwaiter().GetResult();

            Assert.IsNotNull(presenter);
            Assert.AreEqual("new_address", _service.GetLoadedPresenters()[0].Address);
        }

        [UnityTest]
        // ADMIT: UiService.AddUiSet must actually insert the set into _uiSets; the TryAdd both stores it and reports
        // the duplicate, so a guard that only checks membership registers nothing.
        // RCR: UiService.cs AddUiSet — replace `if (!_uiSets.TryAdd(uiSet.SetId, uiSet))` with
        // `if (_uiSets.ContainsKey(uiSet.SetId))` → RED (UiSets.ContainsKey(1) was False). 2026-08-02
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
        // ADMIT: UiService.GetUi<T>(string) must reject an unloaded type with KeyNotFoundException rather than return
        // a null presenter the caller then dereferences.
        // RCR: UiService.cs GetUi<T>(string) — change the throw to `InvalidOperationException` → RED (Assert.Throws
        // reports InvalidOperationException where KeyNotFoundException was expected). 2026-08-02
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
        // ADMIT: UiService.UnloadUi(Type, string) must reject an unloaded instance with KeyNotFoundException instead of
        // silently succeeding and hiding a lifecycle bug in the caller.
        // RCR: UiService.cs UnloadUi(Type, string) — change the throw to `InvalidOperationException` → RED
        // (Assert.Throws reports InvalidOperationException where KeyNotFoundException was expected). 2026-08-02
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
        // ADMIT: UiService.RemoveUiSet must reject an unknown setId with KeyNotFoundException; it is the same contract
        // OpenUiSetAsync states, and callers branch on the type.
        // RCR: UiService.cs RemoveUiSet — change the throw to `InvalidOperationException` → RED (Assert.Throws reports
        // InvalidOperationException). Anchored on RemoveUiSet's own copy of the message, not OpenUiSetAsync's. 2026-08-02
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
        // ADMIT: UiService.Dispose must stay safe on a second call; consumers dispose from both
        // teardown and a scene unload.
        // RCR: none exists - a second Dispose is blocked by both the `if (_disposed) return;` guard and
        // the already-empty _uiPresenters/_visibleUiList plus the null _uiParent; disabling the flag leaves
        // the cleared state making every branch a no-op (verified). Double-covered, not single-line falsifiable.
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
        // ADMIT: UiService.RemoveUi(Type, string) must report false when the type was never registered, or callers
        // treat a no-op as a successful detach and never load the presenter they need.
        // RCR: UiService.cs RemoveUi(Type, string) — change the `_uiPresenters.TryGetValue` guard's `return false;`
        // to `return true;` → RED (Assert.IsFalse fails). The loop's trailing return is a separate mutation. 2026-08-02
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
        // ADMIT: UiService.IsVisible<T>(string) must answer false for a type that was never opened, or
        // callers skip a load they need.
        // RCR: UiService.cs IsVisible<T>(string) - `return _visibleUiList.Contains(instanceId);` ->
        // `return _visibleUiList.Count == 0 || _visibleUiList.Contains(instanceId);` -> RED (expected False, was True).
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
        // ADMIT: exercises UiService.LoadUiAsync/OpenUiAsync/CloseUi's default-instance-address round trip through UiPresenter.IsOpen; no unique one-line pin.
        // RCR: no isolated mutation - reddens under LoadUiAsync_WithoutAddress_UsesConfigAddress's mutation (radius 7, verified).
        // Shared-path coverage, not a duplicate.
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
        // ADMIT: UiService.AddUi must register the instance for GetUi<T> and must NOT open it when
        // openAfter is left at its default false.
        // RCR: UiService.cs AddUi(T, int, string, bool) - `if (openAfter)` -> `if (true)` -> RED
        // (IsVisible<TestUiPresenter>() expected False, was True).
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
        // ADMIT: UiService.AddUi's duplicate check must be keyed by type AND instance address; a type-only check
        // rejects the second instance of a legitimately multi-instance presenter.
        // RCR: UiService.cs AddUi(T, int, string, bool) — replace `if (TryFindPresenter(type, instanceAddress, out _))`
        // with `if (_uiPresenters.ContainsKey(type))` → RED (GetUi<T>("instance_b") throws KeyNotFoundException). 2026-08-02
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

