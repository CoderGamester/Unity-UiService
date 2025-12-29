using UnityEngine;
using UnityEngine.UIElements;

namespace GameLovers.UiService.Tests.PlayMode.Fixtures
{
	/// <summary>
	/// Test presenter with UiToolkitPresenterFeature
	/// </summary>
	[RequireComponent(typeof(UiToolkitPresenterFeature))]
	[RequireComponent(typeof(UIDocument))]
	public class TestUiToolkitPresenter : UiPresenter
	{
		public UiToolkitPresenterFeature ToolkitFeature { get; private set; }
		public bool WasOpened { get; private set; }

		private void Awake()
		{
			// Ensure UIDocument exists
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

			// Set document reference via reflection
			var docField = typeof(UiToolkitPresenterFeature).GetField("_document",
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			docField?.SetValue(ToolkitFeature, document);
		}

		protected override void OnOpened()
		{
			WasOpened = true;
		}
	}
}

