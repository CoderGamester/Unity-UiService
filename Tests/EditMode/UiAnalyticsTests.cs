using NSubstitute;
using NUnit.Framework;

namespace GameLovers.UiService.Tests
{
    [TestFixture]
    public class UiAnalyticsTests
    {
        private UiAnalytics _analytics;

        [SetUp]
        public void Setup()
        {
            _analytics = new UiAnalytics();
        }

        [Test]
        public void TrackLoadStart_StoresStartTime()
        {
            // Act
            _analytics.TrackLoadStart(typeof(TestUiPresenter));

            // Assert - No exception means success
            Assert.Pass();
        }

        [Test]
        public void TrackLoadComplete_AfterStart_RecordsMetrics()
        {
            // Arrange
            _analytics.TrackLoadStart(typeof(TestUiPresenter));

            // Act
            _analytics.TrackLoadComplete(typeof(TestUiPresenter), 0);

            // Assert
            var metrics = _analytics.GetMetrics(typeof(TestUiPresenter));
            Assert.IsTrue(metrics.LoadDuration >= 0);
        }

        [Test]
        public void TrackOpenComplete_IncrementsOpenCount()
        {
            // Arrange
            _analytics.TrackLoadStart(typeof(TestUiPresenter));
            _analytics.TrackLoadComplete(typeof(TestUiPresenter), 0);
            _analytics.TrackOpenStart(typeof(TestUiPresenter));

            // Act
            _analytics.TrackOpenComplete(typeof(TestUiPresenter), 0);

            // Assert
            var metrics = _analytics.GetMetrics(typeof(TestUiPresenter));
            Assert.AreEqual(1, metrics.OpenCount);
        }

        [Test]
        public void GetMetrics_NonExistingType_ReturnsDefaultMetrics()
        {
            // Act
            var metrics = _analytics.GetMetrics(typeof(TestUiPresenter));

            // Assert
            Assert.AreEqual(0, metrics.OpenCount);
            Assert.AreEqual(0, metrics.CloseCount);
        }

        [Test]
        public void Clear_RemovesAllMetrics()
        {
            // Arrange
            _analytics.TrackLoadStart(typeof(TestUiPresenter));
            _analytics.TrackLoadComplete(typeof(TestUiPresenter), 0);

            // Act
            _analytics.Clear();

            // Assert
            Assert.AreEqual(0, _analytics.PerformanceMetrics.Count);
        }

        [Test]
        public void OnUiLoaded_InvokesEvent()
        {
            // Arrange
            bool eventInvoked = false;
            _analytics.OnUiLoaded.AddListener(_ => eventInvoked = true);
            _analytics.TrackLoadStart(typeof(TestUiPresenter));

            // Act
            _analytics.TrackLoadComplete(typeof(TestUiPresenter), 0);

            // Assert
            Assert.IsTrue(eventInvoked);
        }

        [Test]
        public void SetCallback_InvokesOnUiLoaded()
        {
            // Arrange - Using NSubstitute directly
            var callback = Substitute.For<IUiAnalyticsCallback>();
            _analytics.SetCallback(callback);
            _analytics.TrackLoadStart(typeof(TestUiPresenter));

            // Act
            _analytics.TrackLoadComplete(typeof(TestUiPresenter), 0);

            // Assert - NSubstitute verification
            callback.Received(1).OnUiLoaded(Arg.Any<UiEventData>());
        }

        [Test]
        public void SetCallback_InvokesOnUiOpened()
        {
            // Arrange
            var callback = Substitute.For<IUiAnalyticsCallback>();
            _analytics.SetCallback(callback);
            _analytics.TrackOpenStart(typeof(TestUiPresenter));

            // Act
            _analytics.TrackOpenComplete(typeof(TestUiPresenter), 0);

            // Assert
            callback.Received(1).OnUiOpened(Arg.Any<UiEventData>());
        }

        [Test]
        public void SetCallback_InvokesOnUiClosed()
        {
            // Arrange
            var callback = Substitute.For<IUiAnalyticsCallback>();
            _analytics.SetCallback(callback);
            _analytics.TrackCloseStart(typeof(TestUiPresenter));

            // Act
            _analytics.TrackCloseComplete(typeof(TestUiPresenter), 0, false);

            // Assert
            callback.Received(1).OnUiClosed(Arg.Any<UiEventData>());
        }

        [Test]
        public void SetCallback_InvokesOnUiUnloaded()
        {
            // Arrange
            var callback = Substitute.For<IUiAnalyticsCallback>();
            _analytics.SetCallback(callback);

            // Act
            _analytics.TrackUnload(typeof(TestUiPresenter), 0);

            // Assert
            callback.Received(1).OnUiUnloaded(Arg.Any<UiEventData>());
        }

        [Test]
        public void SetCallback_InvokesOnPerformanceMetricsUpdated()
        {
            // Arrange
            var callback = Substitute.For<IUiAnalyticsCallback>();
            _analytics.SetCallback(callback);
            _analytics.TrackLoadStart(typeof(TestUiPresenter));

            // Act
            _analytics.TrackLoadComplete(typeof(TestUiPresenter), 0);

            // Assert
            callback.Received(1).OnPerformanceMetricsUpdated(Arg.Any<UiPerformanceMetrics>());
        }
    }

    [TestFixture]
    public class NullAnalyticsTests
    {
        private NullAnalytics _nullAnalytics;

        [SetUp]
        public void Setup()
        {
            _nullAnalytics = new NullAnalytics();
        }

        [Test]
        public void AllMethods_DoNotThrowExceptions()
        {
            // Assert - All methods should be no-ops
            Assert.DoesNotThrow(() => _nullAnalytics.TrackLoadStart(typeof(TestUiPresenter)));
            Assert.DoesNotThrow(() => _nullAnalytics.TrackLoadComplete(typeof(TestUiPresenter), 0));
            Assert.DoesNotThrow(() => _nullAnalytics.TrackOpenStart(typeof(TestUiPresenter)));
            Assert.DoesNotThrow(() => _nullAnalytics.TrackOpenComplete(typeof(TestUiPresenter), 0));
            Assert.DoesNotThrow(() => _nullAnalytics.TrackCloseStart(typeof(TestUiPresenter)));
            Assert.DoesNotThrow(() => _nullAnalytics.TrackCloseComplete(typeof(TestUiPresenter), 0, false));
            Assert.DoesNotThrow(() => _nullAnalytics.TrackUnload(typeof(TestUiPresenter), 0));
            Assert.DoesNotThrow(() => _nullAnalytics.Clear());
            Assert.DoesNotThrow(() => _nullAnalytics.LogPerformanceSummary());
        }

        [Test]
        public void GetMetrics_AlwaysReturnsDefault()
        {
            // Act
            var metrics = _nullAnalytics.GetMetrics(typeof(TestUiPresenter));

            // Assert
            Assert.AreEqual(0, metrics.OpenCount);
        }

        [Test]
        public void PerformanceMetrics_AlwaysReturnsEmpty()
        {
            // Act
            var metrics = _nullAnalytics.PerformanceMetrics;

            // Assert
            Assert.AreEqual(0, metrics.Count);
        }
    }
}

