using System.Collections.Generic;

namespace GameLovers.UiService.Tests.PlayMode
{
	/// <summary>
	/// Subclass of <see cref="AnimationDelayFeature"/> that records the order in which
	/// the protected lifecycle hooks fire. Used by lifecycle-order tests.
	/// </summary>
	public class TrackingAnimationDelayFeature : AnimationDelayFeature
	{
		public List<string> RecordedHookOrder { get; } = new List<string>();

		protected override void OnOpenStarted()
		{
			RecordedHookOrder.Add(nameof(OnOpenStarted));
			base.OnOpenStarted();
		}

		protected override void OnOpenedCompleted()
		{
			RecordedHookOrder.Add(nameof(OnOpenedCompleted));
			base.OnOpenedCompleted();
		}

		protected override void OnCloseStarted()
		{
			RecordedHookOrder.Add(nameof(OnCloseStarted));
			base.OnCloseStarted();
		}

		protected override void OnClosedCompleted()
		{
			RecordedHookOrder.Add(nameof(OnClosedCompleted));
			base.OnClosedCompleted();
		}
	}
}
