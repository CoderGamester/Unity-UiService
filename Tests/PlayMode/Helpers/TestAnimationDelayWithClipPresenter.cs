using UnityEngine;

namespace GameLovers.UiService.Tests.PlayMode
{
	[RequireComponent(typeof(AnimationDelayFeature))]
	[RequireComponent(typeof(Animation))]
	public class TestAnimationDelayWithClipPresenter : UiPresenter
	{
		public AnimationDelayFeature AnimationFeature { get; private set; }

		private void Awake()
		{
			var animation = GetComponent<Animation>();
			if (animation == null)
			{
				animation = gameObject.AddComponent<Animation>();
			}

			AnimationFeature = GetComponent<AnimationDelayFeature>();
			if (AnimationFeature == null)
			{
				AnimationFeature = gameObject.AddComponent<AnimationDelayFeature>();
			}

			var introClip = new AnimationClip { legacy = true };
			introClip.SetCurve("", typeof(Transform), "localPosition.x",
				AnimationCurve.Linear(0f, 0f, 0.1f, 1f));

			AnimationFeature.SetAnimation(animation, introClip);
		}
	}
}
