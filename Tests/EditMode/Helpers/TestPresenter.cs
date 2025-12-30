using GameLovers.UiService;

namespace GameLovers.UiService.Tests
{
	/// <summary>
	/// Simple test presenter for basic tests
	/// </summary>
	public class TestUiPresenter : UiPresenter
	{
		public bool WasInitialized { get; private set; }
		public bool WasOpened { get; private set; }
		public bool WasClosed { get; private set; }
		public bool WasOpenTransitionCompleted { get; private set; }
		public bool WasCloseTransitionCompleted { get; private set; }
		public int OpenCount { get; private set; }
		public int CloseCount { get; private set; }
		public int OpenTransitionCompletedCount { get; private set; }
		public int CloseTransitionCompletedCount { get; private set; }

		protected override void OnInitialized()
		{
			WasInitialized = true;
		}

		protected override void OnOpened()
		{
			WasOpened = true;
			OpenCount++;
		}

		protected override void OnClosed()
		{
			WasClosed = true;
			CloseCount++;
		}

		protected override void OnOpenTransitionCompleted()
		{
			WasOpenTransitionCompleted = true;
			OpenTransitionCompletedCount++;
		}

		protected override void OnCloseTransitionCompleted()
		{
			WasCloseTransitionCompleted = true;
			CloseTransitionCompletedCount++;
		}
	}
	
	/// <summary>
	/// Test presenter with data support
	/// </summary>
	public struct TestPresenterData
	{
		public int Id;
		public string Name;
	}

	public class TestDataUiPresenter : UiPresenter<TestPresenterData>
	{
		public bool WasDataSet { get; private set; }
		public TestPresenterData ReceivedData { get; private set; }

		protected override void OnSetData()
		{
			WasDataSet = true;
			ReceivedData = Data;
		}
	}
}

