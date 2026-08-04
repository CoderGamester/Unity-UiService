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

			// Animation.Play() with no arguments plays the component's "default clip", which requires the
			// clip to already be registered in the component's attached-animations list. Merely assigning
			// AnimationDelayFeature's `_animation.clip = introClip` later does not retroactively register a
			// freshly-created runtime clip that was never added via AddClip — Play() then logs "Default clip
			// could not be found in attached animations list." and silently no-ops instead of playing.
			animation.AddClip(introClip, introClip.name);

			AnimationFeature.SetAnimation(animation, introClip);
		}
	}
}
