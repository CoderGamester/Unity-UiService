using Cysharp.Threading.Tasks;

namespace GameLovers.UiService.Tests.PlayMode
{
	/// <summary>
	/// Mock feature that tracks lifecycle and can simulate transitions.
	/// Implements ITransitionFeature to allow tests to control when transitions complete.
	/// </summary>
	public class TrackingFeature : PresenterFeatureBase, ITransitionFeature
	{
		private static int _globalOpenCounter;
		
		private UniTaskCompletionSource _openTransitionCompletion;
		private UniTaskCompletionSource _closeTransitionCompletion;
		
		public bool WasInitialized { get; private set; }
		public bool WasOpening { get; private set; }
		public bool WasOpened { get; private set; }
		public bool WasClosing { get; private set; }
		public bool WasClosed { get; private set; }
		public int OpenOrder { get; private set; }
		
		/// <summary>
		/// When true, this feature will start a transition that must be manually completed.
		/// When false, transitions complete immediately.
		/// </summary>
		public bool SimulateDelayedTransitions { get; set; }

		/// <inheritdoc />
		public UniTask OpenTransitionTask => _openTransitionCompletion?.Task ?? UniTask.CompletedTask;

		/// <inheritdoc />
		public UniTask CloseTransitionTask => _closeTransitionCompletion?.Task ?? UniTask.CompletedTask;

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
			
			if (SimulateDelayedTransitions)
			{
				_openTransitionCompletion = new UniTaskCompletionSource();
			}
		}

		public override void OnPresenterClosing()
		{
			WasClosing = true;
			
			if (SimulateDelayedTransitions)
			{
				_closeTransitionCompletion = new UniTaskCompletionSource();
			}
		}

		public override void OnPresenterClosed()
		{
			WasClosed = true;
		}

		/// <summary>
		/// Simulates the completion of an open transition.
		/// Only effective when SimulateDelayedTransitions is true.
		/// </summary>
		public void SimulateOpenTransitionComplete()
		{
			_openTransitionCompletion?.TrySetResult();
		}

		/// <summary>
		/// Simulates the completion of a close transition.
		/// Only effective when SimulateDelayedTransitions is true.
		/// </summary>
		public void SimulateCloseTransitionComplete()
		{
			_closeTransitionCompletion?.TrySetResult();
		}

		public void Reset()
		{
			WasInitialized = false;
			WasOpening = false;
			WasOpened = false;
			WasClosing = false;
			WasClosed = false;
			_openTransitionCompletion = null;
			_closeTransitionCompletion = null;
		}
	}
}
