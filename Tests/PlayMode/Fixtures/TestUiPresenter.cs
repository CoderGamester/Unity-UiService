namespace GameLovers.UiService.Tests.PlayMode.Fixtures
{
	/// <summary>
	/// Simple test presenter for basic tests.
	/// This class is in a runtime-compatible assembly so it can be added to prefabs.
	/// </summary>
	public class TestUiPresenter : UiPresenter
	{
		public bool WasInitialized { get; private set; }
		public bool WasOpened { get; private set; }
		public bool WasClosed { get; private set; }
		public int OpenCount { get; private set; }
		public int CloseCount { get; private set; }

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
	}
}

