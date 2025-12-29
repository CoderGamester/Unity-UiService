using UnityEngine;

namespace GameLovers.UiService.Tests.PlayMode.Fixtures
{
	/// <summary>
	/// Test presenter with AnimationDelayFeature for testing animation behavior
	/// </summary>
	[RequireComponent(typeof(AnimationDelayFeature))]
	[RequireComponent(typeof(Animation))]
	public class TestAnimationDelayPresenter : UiPresenter
	{
		public AnimationDelayFeature AnimationFeature { get; private set; }
		public bool WasOpenTransitionCompleted { get; private set; }
		public bool WasCloseTransitionCompleted { get; private set; }

		private void Awake()
		{
			// Ensure Animation component exists
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

			// Set animation reference via reflection
			var animField = typeof(AnimationDelayFeature).GetField("_animation",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			animField?.SetValue(AnimationFeature, animation);
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

