using UnityEngine;

namespace GameLovers.UiService.Tests.PlayMode
{
	[RequireComponent(typeof(TrackingAnimationDelayFeature))]
	[RequireComponent(typeof(Animation))]
	public class TestTrackingAnimationDelayPresenter : UiPresenter
	{
		public TrackingAnimationDelayFeature AnimationFeature { get; private set; }

		private void Awake()
		{
			var animation = GetComponent<Animation>();
			if (animation == null)
			{
				animation = gameObject.AddComponent<Animation>();
			}

			AnimationFeature = GetComponent<TrackingAnimationDelayFeature>();
			if (AnimationFeature == null)
			{
				AnimationFeature = gameObject.AddComponent<TrackingAnimationDelayFeature>();
			}

			var introClip = new AnimationClip { legacy = true };
			introClip.SetCurve(string.Empty, typeof(Transform), "localPosition.x",
				AnimationCurve.Linear(0f, 0f, 0.05f, 1f));

			var outroClip = new AnimationClip { legacy = true };
			outroClip.SetCurve(string.Empty, typeof(Transform), "localPosition.x",
				AnimationCurve.Linear(0f, 1f, 0.05f, 0f));

			AnimationFeature.SetAnimation(animation, introClip, outroClip);
		}
	}
}
