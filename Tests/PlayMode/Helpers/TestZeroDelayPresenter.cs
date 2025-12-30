using UnityEngine;

namespace GameLovers.UiService.Tests.PlayMode
{
	/// <summary>
	/// Test presenter with zero delay for edge case testing
	/// </summary>
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
			
			// Set zero delays
			var openField = typeof(TimeDelayFeature).GetField("_openDelayInSeconds", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var closeField = typeof(TimeDelayFeature).GetField("_closeDelayInSeconds", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			openField?.SetValue(DelayFeature, 0f);
			closeField?.SetValue(DelayFeature, 0f);
		}

		protected override void OnOpenTransitionCompleted()
		{
			WasOpenTransitionCompleted = true;
		}
	}
}

