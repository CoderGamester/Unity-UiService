using UnityEngine;

namespace GameLovers.UiService.Tests.PlayMode.Fixtures
{
	/// <summary>
	/// Test presenter with animation clips for testing clip-based delays
	/// </summary>
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

			// Create a mock animation clip with known length
			var introClip = new AnimationClip();
			introClip.legacy = true;
			
			// Add a dummy curve to give it length
			introClip.SetCurve("", typeof(Transform), "localPosition.x", 
				AnimationCurve.Linear(0f, 0f, 0.1f, 1f));

			// Set fields via reflection
			var animField = typeof(AnimationDelayFeature).GetField("_animation",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var introField = typeof(AnimationDelayFeature).GetField("_introAnimationClip",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			
			animField?.SetValue(AnimationFeature, animation);
			introField?.SetValue(AnimationFeature, introClip);
		}
	}
}

