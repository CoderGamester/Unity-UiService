namespace GameLovers.UiService.Tests.PlayMode
{
	/// <summary>
	/// Mock feature that tracks lifecycle and can simulate transitions
	/// </summary>
	public class TrackingFeature : PresenterFeatureBase
	{
		private static int _globalOpenCounter;
		
		public bool WasInitialized { get; private set; }
		public bool WasOpening { get; private set; }
		public bool WasOpened { get; private set; }
		public bool WasClosing { get; private set; }
		public bool WasClosed { get; private set; }
		public int OpenOrder { get; private set; }

		public override void OnPresenterInitialized(UiPresenter presenter)
		{
			base.OnPresenterInitialized(presenter);
			WasInitialized = true;
		}

		public override void OnPresenterOpening()
		{
			WasOpening = true;
		}

		public override void OnPresenterOpened()
		{
			WasOpened = true;
			OpenOrder = _globalOpenCounter++;
		}

		public override void OnPresenterClosing()
		{
			WasClosing = true;
		}

		public override void OnPresenterClosed()
		{
			WasClosed = true;
		}

		public void SimulateOpenTransitionComplete()
		{
			Presenter.NotifyOpenTransitionCompleted();
		}

		public void SimulateCloseTransitionComplete()
		{
			Presenter.NotifyCloseTransitionCompleted();
		}

		public void Reset()
		{
			WasInitialized = false;
			WasOpening = false;
			WasOpened = false;
			WasClosing = false;
			WasClosed = false;
		}
	}
}

