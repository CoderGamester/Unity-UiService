using UnityEngine;

namespace GameLovers.UiService.Tests.PlayMode
{
	[RequireComponent(typeof(TrackingTimeDelayFeature))]
	public class TestTrackingTimeDelayPresenter : UiPresenter
	{
		public TrackingTimeDelayFeature DelayFeature { get; private set; }

		private void Awake()
		{
			DelayFeature = GetComponent<TrackingTimeDelayFeature>();
			if (DelayFeature == null)
			{
				DelayFeature = gameObject.AddComponent<TrackingTimeDelayFeature>();
			}

			DelayFeature.SetDelays(0.05f, 0.05f);
		}
	}
}
