using UnityEngine;
using UnityEngine.UIElements;

namespace GameLovers.UiService.Tests.PlayMode
{
	/// <summary>
	/// Test presenter pairing UiToolkitPresenterFeature with UiPanelSettingsFeature. Follows
	/// TestUiToolkitPresenter's pattern for a real, warning-free PanelSettings + theme in the
	/// test environment; exposes SharedPanelSettings so tests can assert the shared asset was
	/// never mutated.
	/// </summary>
	[RequireComponent(typeof(UIDocument))]
	[RequireComponent(typeof(UiToolkitPresenterFeature))]
	[RequireComponent(typeof(UiPanelSettingsFeature))]
	public class TestPanelSettingsPresenter : UiPresenter
	{
		public UiToolkitPresenterFeature ToolkitFeature { get; private set; }
		public UiPanelSettingsFeature PanelSettingsFeature { get; private set; }
		public PanelSettings SharedPanelSettings { get; private set; }

		private void Awake()
		{
			var document = GetComponent<UIDocument>();
			if (document == null)
			{
				document = gameObject.AddComponent<UIDocument>();
			}

			if (document.panelSettings == null)
			{
				SharedPanelSettings = ScriptableObject.CreateInstance<PanelSettings>();
				SharedPanelSettings.themeStyleSheet = ScriptableObject.CreateInstance<ThemeStyleSheet>();
				document.panelSettings = SharedPanelSettings;
			}
			else
			{
				SharedPanelSettings = document.panelSettings;
			}

			ToolkitFeature = GetComponent<UiToolkitPresenterFeature>();
			if (ToolkitFeature == null)
			{
				ToolkitFeature = gameObject.AddComponent<UiToolkitPresenterFeature>();
			}
			ToolkitFeature.SetDocument(document);

			PanelSettingsFeature = GetComponent<UiPanelSettingsFeature>();
			if (PanelSettingsFeature == null)
			{
				PanelSettingsFeature = gameObject.AddComponent<UiPanelSettingsFeature>();
			}
		}
	}
}
