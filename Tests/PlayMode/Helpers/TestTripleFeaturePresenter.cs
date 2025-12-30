using UnityEngine;

namespace GameLovers.UiService.Tests.PlayMode
{
	/// <summary>
	/// Test presenter with three features
	/// </summary>
	public class TestTripleFeaturePresenter : UiPresenter
	{
		public TrackingFeature FeatureA { get; private set; }
		public TrackingFeature FeatureB { get; private set; }
		public TrackingFeature FeatureC { get; private set; }
		public int OpenTransitionCount { get; private set; }

		private void Awake()
		{
			FeatureA = gameObject.AddComponent<TrackingFeature>();
			FeatureB = gameObject.AddComponent<TrackingFeature>();
			FeatureC = gameObject.AddComponent<TrackingFeature>();
		}

		protected override void OnOpenTransitionCompleted()
		{
			OpenTransitionCount++;
		}
	}
}

