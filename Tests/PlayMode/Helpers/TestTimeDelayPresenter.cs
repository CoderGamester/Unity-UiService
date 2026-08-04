using UnityEngine;

namespace GameLovers.UiService.Tests.PlayMode
{
	[RequireComponent(typeof(TimeDelayFeature))]
	public class TestTimeDelayPresenter : UiPresenter
	{
		public TimeDelayFeature DelayFeature { get; private set; }
		public bool WasOpenTransitionCompleted { get; private set; }
		public bool WasCloseTransitionCompleted { get; private set; }

		private void Awake()
		{
			DelayFeature = GetComponent<TimeDelayFeature>();
			if (DelayFeature == null)
			{
				DelayFeature = gameObject.AddComponent<TimeDelayFeature>();
			}

			DelayFeature.SetDelays(0.1f, 0.05f);
		}

		protected override void OnOpenTransitionCompleted()
		{
			WasOpenTransitionCompleted = true;
		}

		protected override void OnCloseTransitionCompleted()
		{
			WasCloseTransitionCompleted = true;
		}
	}
}
