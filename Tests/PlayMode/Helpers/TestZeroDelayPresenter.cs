using UnityEngine;

namespace GameLovers.UiService.Tests.PlayMode
{
	[RequireComponent(typeof(TimeDelayFeature))]
	public class TestZeroDelayPresenter : UiPresenter
	{
		public TimeDelayFeature DelayFeature { get; private set; }
		public bool WasOpenTransitionCompleted { get; private set; }

		private void Awake()
		{
			DelayFeature = GetComponent<TimeDelayFeature>();
			if (DelayFeature == null)
			{
				DelayFeature = gameObject.AddComponent<TimeDelayFeature>();
			}

			DelayFeature.SetDelays(0f, 0f);
		}

		protected override void OnOpenTransitionCompleted()
		{
			WasOpenTransitionCompleted = true;
		}
	}
}
