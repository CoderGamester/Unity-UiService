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
			// Unload after each iteration so every measurement benchmarks an actual
			// Load (not a cache hit that would also log an "already loaded" warning).
			Measure.Method(() =>
			{
				_service.LoadUiAsync(typeof(TestUiPresenter)).GetAwaiter().GetResult();
			})
			.CleanUp(() =>
			{
				_service.UnloadUi(typeof(TestUiPresenter));
			})
			.WarmupCount(1)
			.MeasurementCount(10)
			.Run();

			yield return null;
		}

		[UnityTest, Performance]
		public IEnumerator Perf_LoadUiSet_MultiplePresenters()
		{
			// Unload after each iteration (.CleanUp, not part of the measured body) so every
			// measurement benchmarks an actual Load-of-the-set, matching the
			// Perf_UnloadUi_SinglePresenter reference pattern in this file. Previously the
			// UnloadUiSet(1) call lived inside Measure.Method, so this benchmark measured
			// load+unload under a name implying load-only cost.
			Measure.Method(() =>
			{
				var tasks = _service.LoadUiSetAsync(1);
				foreach (var task in tasks)
				{
					task.GetAwaiter().GetResult();
				}
			})
			.CleanUp(() =>
			{
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
			// SetUp loads fresh before each iteration; body measures only the unload.
			// After the body runs, the presenter is unloaded so the next SetUp's load
			// is always a real load (avoids "already loaded" warnings).
			Measure.Method(() =>
			{
				_service.UnloadUi(typeof(TestUiPresenter));
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
		public IEnumerator Perf_CloseAllUi_DispatchOnly()
		{
			// CloseAllUi's real work is the fire-and-forget InternalCloseProcessAsync per
			// presenter (UiPresenter.InternalClose -> InternalCloseProcessAsync(...).Forget()),
			// which this benchmark does not await. This measures only the synchronous dispatch
			// cost of iterating _visibleUiList and kicking off each presenter's close, not the
			// full close (including any transition features). Renamed from
			// Perf_CloseAllUi_ManyPresenters, which implied full-close cost.
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
	}
}
