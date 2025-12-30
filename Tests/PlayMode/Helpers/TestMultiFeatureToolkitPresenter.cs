using UnityEngine;
using UnityEngine.UIElements;

namespace GameLovers.UiService.Tests.PlayMode
{
	/// <summary>
	/// Test presenter with multiple features (UiToolkit + TimeDelay)
	/// </summary>
	[RequireComponent(typeof(UiToolkitPresenterFeature))]
	[RequireComponent(typeof(TimeDelayFeature))]
	[RequireComponent(typeof(UIDocument))]
	public class TestMultiFeatureToolkitPresenter : UiPresenter
	{
		public UiToolkitPresenterFeature ToolkitFeature { get; private set; }
		public TimeDelayFeature DelayFeature { get; private set; }

		private void Awake()
		{
			var document = GetComponent<UIDocument>();
			if (document == null)
			{
				document = gameObject.AddComponent<UIDocument>();
			}

			ToolkitFeature = GetComponent<UiToolkitPresenterFeature>();
			if (ToolkitFeature == null)
			{
				ToolkitFeature = gameObject.AddComponent<UiToolkitPresenterFeature>();
			}

			DelayFeature = GetComponent<TimeDelayFeature>();
			if (DelayFeature == null)
			{
				DelayFeature = gameObject.AddComponent<TimeDelayFeature>();
			}

			// Set document reference
			var docField = typeof(UiToolkitPresenterFeature).GetField("_document",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			docField?.SetValue(ToolkitFeature, document);

			// Set short delays
			var openField = typeof(TimeDelayFeature).GetField("_openDelayInSeconds",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			openField?.SetValue(DelayFeature, 0.01f);
		}
	}
}

