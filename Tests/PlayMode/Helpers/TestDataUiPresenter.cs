namespace GameLovers.UiService.Tests.PlayMode
{
	/// <summary>
	/// Test presenter data struct
	/// </summary>
	public struct TestPresenterData
	{
		public int Id;
		public string Name;
	}
    
	/// <summary>
	/// Test presenter with data support.
	/// This class is in a runtime-compatible assembly so it can be added to prefabs.
	/// </summary>
	public class TestDataUiPresenter : UiPresenter<TestPresenterData>
	{
		public bool WasDataSet { get; private set; }
		public int OnSetDataCallCount { get; private set; }
		public TestPresenterData ReceivedData { get; private set; }

		protected override void OnSetData()
		{
			WasDataSet = true;
			OnSetDataCallCount++;
			ReceivedData = Data;
		}
	}
}

