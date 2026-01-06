using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine.TestTools;

namespace GameLovers.UiService.Tests.PlayMode
{
	/// <summary>
	/// Performance benchmarks for the UI Service.
	/// </summary>
	[TestFixture]
	public class PerformanceTests
	{
		private MockAssetLoader _mockLoader;
		private UiService _service;

		[SetUp]
		public void Setup()
		{
			_mockLoader = new MockAssetLoader();
			_mockLoader.RegisterPrefab<TestUiPresenter>("test_presenter");
			_mockLoader.RegisterPrefab<TestDataUiPresenter>("data_presenter");
			
			_service = new UiService(_mockLoader);
			
			var set = TestHelpers.CreateTestUiSet(1, 
				new UiInstanceId(typeof(TestUiPresenter), "set_1"),
				new UiInstanceId(typeof(TestUiPresenter), "set_2"),
				new UiInstanceId(typeof(TestDataUiPresenter), "set_3")
			);
			_service.Init(TestHelpers.CreateTestConfigsWithSets(
				new[]
				{
					TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "test_presenter", 0),
					TestHelpers.CreateTestConfig(typeof(TestDataUiPresenter), "data_presenter", 1)
				},
				new[] { set }
			));
		}

		[TearDown]
		public void TearDown()
		{
			_service?.Dispose();
			_mockLoader?.Cleanup();
		}

		[UnityTest, Performance]
		public IEnumerator Perf_LoadUi_SinglePresenter()
		{
			Measure.Method(() =>
			{
				var task = _service.LoadUiAsync(typeof(TestUiPresenter));
				task.GetAwaiter().GetResult();
			})
			.WarmupCount(1)
			.MeasurementCount(10)
			.Run();

			yield return null;
		}

		[UnityTest, Performance]
		public IEnumerator Perf_LoadUiSet_MultiplePresenters()
		{
			Measure.Method(() =>
			{
				var tasks = _service.LoadUiSetAsync(1);
				foreach (var task in tasks)
				{
					task.GetAwaiter().GetResult();
				}
				
				// Cleanup for next measurement
				_service.UnloadUiSet(1);
			})
			.WarmupCount(1)
			.MeasurementCount(10)
			.Run();

			yield return null;
		}

		[UnityTest, Performance]
		public IEnumerator Perf_UnloadUi_SinglePresenter()
		{
			Measure.Method(() =>
			{
				// Measure only the unload part
				using (Measure.Scope())
				{
					_service.UnloadUi(typeof(TestUiPresenter));
				}
				
				// Re-load for next measurement
				_service.LoadUiAsync(typeof(TestUiPresenter)).GetAwaiter().GetResult();
			})
			.SetUp(() =>
			{
				_service.LoadUiAsync(typeof(TestUiPresenter)).GetAwaiter().GetResult();
			})
			.WarmupCount(2)
			.MeasurementCount(20)
			.Run();

			yield return null;
		}

		[UnityTest, Performance]
		public IEnumerator Perf_OpenCloseUi_Cycle()
		{
			// Pre-load
			var loadTask = _service.LoadUiAsync(typeof(TestUiPresenter));
			yield return loadTask.ToCoroutine();

			Measure.Method(() =>
			{
				var openTask = _service.OpenUiAsync(typeof(TestUiPresenter));
				openTask.GetAwaiter().GetResult();
				_service.CloseUi(typeof(TestUiPresenter));
			})
			.WarmupCount(2)
			.MeasurementCount(20)
			.Run();

			yield return null;
		}

		[UnityTest, Performance]
		public IEnumerator Perf_OpenUiWithData_Cycle()
		{
			// Pre-load
			var loadTask = _service.LoadUiAsync(typeof(TestDataUiPresenter));
			yield return loadTask.ToCoroutine();
			var data = new TestPresenterData { Id = 1, Name = "PerfTest" };

			Measure.Method(() =>
			{
				var openTask = _service.OpenUiAsync(typeof(TestDataUiPresenter), data);
				openTask.GetAwaiter().GetResult();
				_service.CloseUi(typeof(TestDataUiPresenter));
			})
			.WarmupCount(2)
			.MeasurementCount(20)
			.Run();

			yield return null;
		}

		[UnityTest, Performance]
		public IEnumerator Perf_CloseAllUi_ManyPresenters()
		{
			Measure.Method(() =>
			{
				_service.CloseAllUi();
			})
			.SetUp(() =>
			{
				for (int i = 0; i < 20; i++)
				{
					_service.OpenUiAsync(typeof(TestUiPresenter), $"instance_{i}").GetAwaiter().GetResult();
				}
			})
			.WarmupCount(2)
			.MeasurementCount(10)
			.Run();

			yield return null;
		}

		[UnityTest, Performance]
		public IEnumerator Perf_GetLoadedPresenters_ManyPresenters()
		{
			// Load many presenters
			for (int i = 0; i < 50; i++)
			{
				var task = _service.LoadUiAsync(typeof(TestUiPresenter), $"instance_{i}");
				yield return task.ToCoroutine();
			}

			Measure.Method(() =>
			{
				_ = _service.GetLoadedPresenters();
			})
			.WarmupCount(5)
			.MeasurementCount(100)
			.Run();

			Measure.Method(() =>
			{
				long start = GC.GetAllocatedBytesForCurrentThread();
				_ = _service.GetLoadedPresenters();
				long end = GC.GetAllocatedBytesForCurrentThread();
				Measure.Custom(new SampleGroup("GetLoadedPresenters.GCAlloc", SampleUnit.Byte), end - start);
			})
			.WarmupCount(5)
			.MeasurementCount(100)
			.Run();

			yield return null;
		}

		[UnityTest, Performance]
		public IEnumerator Perf_IsVisible_Check()
		{
			var task = _service.OpenUiAsync(typeof(TestUiPresenter));
			yield return task.ToCoroutine();

			Measure.Method(() =>
			{
				_service.IsVisible<TestUiPresenter>();
			})
			.WarmupCount(10)
			.MeasurementCount(1000)
			.Run();

			Measure.Method(() =>
			{
				long start = GC.GetAllocatedBytesForCurrentThread();
				_service.IsVisible<TestUiPresenter>();
				long end = GC.GetAllocatedBytesForCurrentThread();
				Measure.Custom(new SampleGroup("IsVisible.GCAlloc", SampleUnit.Byte), end - start);
			})
			.WarmupCount(10)
			.MeasurementCount(100)
			.Run();

			yield return null;
		}

		[UnityTest, Performance]
		public IEnumerator Perf_AnalyticsTracking_Overhead()
		{
			var configs = TestHelpers.CreateTestConfigs(
				TestHelpers.CreateTestConfig(typeof(TestUiPresenter), "test_presenter", 0)
			);
			
			using var mockService = new UiService(_mockLoader, new MockAnalytics());
			mockService.Init(configs);
			var loadTask1 = mockService.LoadUiAsync(typeof(TestUiPresenter));
			yield return loadTask1.ToCoroutine();

			Measure.Method(() =>
			{
				mockService.OpenUiAsync(typeof(TestUiPresenter)).GetAwaiter().GetResult();
				mockService.CloseUi(typeof(TestUiPresenter));
			})
			.SampleGroup("MockAnalytics")
			.WarmupCount(2)
			.MeasurementCount(20)
			.Run();

			using var nullService = new UiService(_mockLoader, new NullAnalytics());
			nullService.Init(configs);
			var loadTask2 = nullService.LoadUiAsync(typeof(TestUiPresenter));
			yield return loadTask2.ToCoroutine();

			Measure.Method(() =>
			{
				nullService.OpenUiAsync(typeof(TestUiPresenter)).GetAwaiter().GetResult();
				nullService.CloseUi(typeof(TestUiPresenter));
			})
			.SampleGroup("NullAnalytics")
			.WarmupCount(2)
			.MeasurementCount(20)
			.Run();
		}
	}
}
