using UnityEngine;
using UnityEngine.UIElements;

namespace GameLovers.UiService.Tests.PlayMode
{
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

			// Empty ThemeStyleSheet must be assigned BEFORE panelSettings is set on the document,
			// otherwise Unity logs the "No Theme Style Sheet set to PanelSettings" warning twice
			// (once on assignment and again on the first SetActive(true)).
			if (document.panelSettings == null)
			{
				var panel = ScriptableObject.CreateInstance<PanelSettings>();
				panel.themeStyleSheet = ScriptableObject.CreateInstance<ThemeStyleSheet>();
				document.panelSettings = panel;
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

			ToolkitFeature.SetDocument(document);
			DelayFeature.SetDelays(0.01f, DelayFeature.CloseDelayInSeconds);
		}
	}
}
