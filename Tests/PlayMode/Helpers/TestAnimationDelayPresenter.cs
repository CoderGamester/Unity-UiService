using UnityEngine;

namespace GameLovers.UiService.Tests.PlayMode
{
	[RequireComponent(typeof(AnimationDelayFeature))]
	[RequireComponent(typeof(Animation))]
	public class TestAnimationDelayPresenter : UiPresenter
	{
		public AnimationDelayFeature AnimationFeature { get; private set; }
		public bool WasOpenTransitionCompleted { get; private set; }
		public bool WasCloseTransitionCompleted { get; private set; }

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

			AnimationFeature.SetAnimation(animation);
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
