using UnityEngine;

namespace GameLovers.UiService.Tests.PlayMode.Fixtures
{
	/// <summary>
	/// Test presenter with TimeDelayFeature for testing delay behavior
	/// </summary>
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
			
			// Set short delays for testing
			SetDelayValues(0.1f, 0.05f);
		}

		private void SetDelayValues(float open, float close)
		{
			var openField = typeof(TimeDelayFeature).GetField("_openDelayInSeconds", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var closeField = typeof(TimeDelayFeature).GetField("_closeDelayInSeconds", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			openField?.SetValue(DelayFeature, open);
			closeField?.SetValue(DelayFeature, close);
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

